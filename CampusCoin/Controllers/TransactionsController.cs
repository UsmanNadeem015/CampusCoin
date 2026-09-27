using CampusCoin.Services;
using System.Security.Claims;
using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly CampusCoinDbContext _context;
    private readonly AICategorizationService _aiCategorizationService;

    public TransactionsController(
        CampusCoinDbContext context,
        AICategorizationService aiCategorizationService)
    {
        _context = context;
        _aiCategorizationService = aiCategorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.TransactionId)
            .ToListAsync();

        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var categories = await _context.Categories
            .Where(c => c.IsDefault || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        var transaction = new Transaction
        {
            TransactionDate = DateTime.Today,
            TransactionType = TransactionType.Expense
        };

        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction input)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        input.UserId = userId;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryId == input.CategoryId &&
                (c.IsDefault || c.UserId == userId));

        if (category == null)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid category."
            );
        }

        if (category != null &&
            (int)category.Type != (int)input.TransactionType)
        {
            ModelState.AddModelError(
                "CategoryId",
                "The selected category does not match the transaction type."
            );
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsDefault || c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(input);
        }

        input.CreatedAt = DateTime.UtcNow;

        input.NextRecurringDate = input.IsRecurring
            ? input.TransactionDate.AddMonths(1)
            : null;

        _context.Transactions.Add(input);

        await _context.SaveChangesAsync();

        await RefreshBudgetNotificationAsync(
            userId,
            input.CategoryId,
            input.TransactionDate
        );

        return RedirectToAction(nameof(Index));
    }
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestCategory(
        [FromBody] CategorySuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new
            {
                success = false,
                message = "A transaction description is required."
            });
        }

        if (!Enum.IsDefined(typeof(TransactionType), request.TransactionType))
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid transaction type."
            });
        }

        var categories = await _context.Categories
            .Where(c =>
                c.IsDefault ||
                c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return Ok(new
            {
                success = false,
                message = "No categories are available."
            });
        }

        var suggestedCategoryId =
            await _aiCategorizationService.SuggestCategoryAsync(
                request.Description,
                request.TransactionType,
                categories,
                cancellationToken);

        if (!suggestedCategoryId.HasValue)
        {
            return Ok(new
            {
                success = false,
                message = "No suitable category suggestion was found."
            });
        }

        var suggestedCategory = categories
            .FirstOrDefault(c =>
                c.CategoryId == suggestedCategoryId.Value);

        if (suggestedCategory == null)
        {
            return Ok(new
            {
                success = false,
                message = "The suggested category is not available."
            });
        }

        return Ok(new
        {
            success = true,
            categoryId = suggestedCategory.CategoryId,
            categoryName = suggestedCategory.Name
        });
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.TransactionId == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        var categories = await _context.Categories
            .Where(c => c.IsDefault || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Transaction input)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.TransactionId == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        var oldCategoryId = transaction.CategoryId;
        var oldTransactionDate = transaction.TransactionDate;

        var validCategory = await _context.Categories
            .AnyAsync(c =>
                (c.UserId == null || c.UserId == userId) &&
                c.CategoryId == input.CategoryId &&
                (int)c.Type == (int)input.TransactionType);

        if (!validCategory)
        {
            ModelState.AddModelError(
                "CategoryId",
                "The selected category is not valid for this transaction type.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsDefault || c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(input);
        }

        transaction.CategoryId = input.CategoryId;
        transaction.Amount = input.Amount;
        transaction.TransactionType = input.TransactionType;
        transaction.Description = input.Description;
        transaction.TransactionDate = input.TransactionDate;
        transaction.IsRecurring = input.IsRecurring;
        transaction.NextRecurringDate =
            input.IsRecurring
                ? input.NextRecurringDate
                : null;

        await _context.SaveChangesAsync();

        await RefreshBudgetNotificationAsync(
            userId,
            oldCategoryId,
            oldTransactionDate);

        await RefreshBudgetNotificationAsync(
            userId,
            transaction.CategoryId,
            transaction.TransactionDate);

        return RedirectToAction(nameof(Index));
    }
    // Edit end

    // Delete start
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t =>
                t.TransactionId == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.TransactionId == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        var categoryId = transaction.CategoryId;
        var transactionDate = transaction.TransactionDate;

        _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();

        await RefreshBudgetNotificationAsync(
            userId,
            categoryId,
            transactionDate);

        return RedirectToAction(nameof(Index));
    }
    // Delete end

    // Details start
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t =>
                t.TransactionId == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }
    // Details end

    private async Task RefreshBudgetNotificationAsync(
        int userId,
        int categoryId,
        DateTime transactionDate)
    {
        var monthStart = new DateTime(
            transactionDate.Year,
            transactionDate.Month,
            1);

        var nextMonthStart = monthStart.AddMonths(1);

        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Month == monthStart);

        if (budget == null)
            return;

        var actualSpent = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.CategoryId == categoryId &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        if (budget.LimitAmount <= 0)
            return;

        var usagePercentage =
            (actualSpent / budget.LimitAmount) * 100m;

        NotificationType? notificationType = null;

        if (actualSpent >= budget.LimitAmount)
        {
            notificationType = NotificationType.BudgetExceeded;
        }
        else if (usagePercentage >= 80m)
        {
            notificationType = NotificationType.BudgetNearLimit;
        }

        var categoryName = budget.Category?.Name ?? "Category";

        var nearLimitTitle =
            $"Budget Near Limit - {categoryName} - {monthStart:MMMM yyyy}";

        var exceededTitle =
            $"Budget Exceeded - {categoryName} - {monthStart:MMMM yyyy}";

        // No alert is needed anymore.
        // Remove any old notification for this budget.
        if (notificationType == null)
        {
            var oldNotifications = await _context.Notifications
                .Where(n =>
                    n.UserId == userId &&
                    (n.Title == nearLimitTitle ||
                     n.Title == exceededTitle))
                .ToListAsync();

            if (oldNotifications.Count > 0)
            {
                _context.Notifications.RemoveRange(oldNotifications);

                await _context.SaveChangesAsync();
            }

            return;
        }

        string title;
        string message;

        if (notificationType == NotificationType.BudgetExceeded)
        {
            title = exceededTitle;

            message =
                $"Your {categoryName} budget has been exceeded. " +
                $"You have spent Rs. {actualSpent:N2} " +
                $"out of Rs. {budget.LimitAmount:N2}.";
        }
        else
        {
            title = nearLimitTitle;

            message =
                $"Your {categoryName} budget is at {usagePercentage:N1}%. " +
                $"You have spent Rs. {actualSpent:N2} " +
                $"out of Rs. {budget.LimitAmount:N2}.";
        }

        var oppositeTitle =
            notificationType == NotificationType.BudgetExceeded
                ? nearLimitTitle
                : exceededTitle;

        var oppositeNotifications = await _context.Notifications
            .Where(n =>
                n.UserId == userId &&
                n.Title == oppositeTitle)
            .ToListAsync();

        if (oppositeNotifications.Count > 0)
        {
            _context.Notifications.RemoveRange(oppositeNotifications);
        }

        var existingNotification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.UserId == userId &&
                n.Title == title);

        if (existingNotification == null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = notificationType.Value,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingNotification.Message = message;
            existingNotification.IsRead = false;
        }

        await _context.SaveChangesAsync();
    }
    public class CategorySuggestionRequest
    {
        public string Description { get; set; } = string.Empty;

        public TransactionType TransactionType { get; set; }
    }
}