using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class BudgetsController : Controller
{
    private readonly CampusCoinDbContext _context;

    public BudgetsController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var budgets = await _context.Budgets
            .Where(b => b.UserId == userId)
            .Include(b => b.Category)
            .OrderByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync();

        var summaries = new List<BudgetSummary>();

        foreach (var budget in budgets)
        {
            var monthStart = new DateTime(
                budget.Month.Year,
                budget.Month.Month,
                1
            );

            var nextMonthStart = monthStart.AddMonths(1);

            var actualSpent = await _context.Transactions
                .Where(t =>
                    t.UserId == userId &&
                    t.CategoryId == budget.CategoryId &&
                    t.TransactionType == TransactionType.Expense &&
                    t.TransactionDate >= monthStart &&
                    t.TransactionDate < nextMonthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var remaining = budget.LimitAmount - actualSpent;

            var percentage = budget.LimitAmount > 0
                ? (actualSpent / budget.LimitAmount) * 100m
                : 0m;

            summaries.Add(new BudgetSummary
            {
                BudgetId = budget.BudgetId,
                CategoryName = budget.Category?.Name ?? "-",
                Month = budget.Month,
                LimitAmount = budget.LimitAmount,
                ActualSpent = actualSpent,
                Remaining = remaining,
                UsagePercentage = percentage
            });
        }

        return View(summaries);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var categories = await _context.Categories
            .Where(c =>
                c.Type == CategoryType.Expense &&
                (c.IsDefault || c.UserId == userId))
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        var input = new BudgetInput
        {
            Month = DateTime.Today.ToString("yyyy-MM"),
            LimitAmount = 0
        };

        return View(input);
    }

    public class BudgetSummary
    {
        public int BudgetId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public DateTime Month { get; set; }

        public decimal LimitAmount { get; set; }

        public decimal ActualSpent { get; set; }

        public decimal Remaining { get; set; }

        public decimal UsagePercentage { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BudgetInput input)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        if (!DateTime.TryParseExact(
                input.Month,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime month))
        {
            ModelState.AddModelError(
                "Month",
                "Please select a valid month."
            );
        }

        month = new DateTime(
            month.Year,
            month.Month,
            1
        );

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryId == input.CategoryId &&
                c.Type == CategoryType.Expense &&
                (c.IsDefault || c.UserId == userId));

        if (category == null)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid expense category."
            );
        }

        if (category != null)
        {
            var budgetExists = await _context.Budgets
                .AnyAsync(b =>
                    b.UserId == userId &&
                    b.CategoryId == input.CategoryId &&
                    b.Month == month);

            if (budgetExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "A budget already exists for this category and month."
                );
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c =>
                    c.Type == CategoryType.Expense &&
                    (c.IsDefault || c.UserId == userId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(input);
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = input.CategoryId,
            Month = month,
            LimitAmount = input.LimitAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(budget);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public class BudgetInput
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Month { get; set; } = string.Empty;

        [Range(0.01, 999999999.99)]
        public decimal LimitAmount { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b =>
                b.BudgetId == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        return View(budget);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.BudgetId == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        _context.Budgets.Remove(budget);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.BudgetId == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        var categories = await _context.Categories
            .Where(c =>
                c.Type == CategoryType.Expense &&
                (c.IsDefault || c.UserId == userId))
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        var input = new BudgetInput
        {
            CategoryId = budget.CategoryId,
            Month = budget.Month.ToString("yyyy-MM"),
            LimitAmount = budget.LimitAmount
        };

        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BudgetInput input)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.BudgetId == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        DateTime month = DateTime.MinValue;

        if (!DateTime.TryParseExact(
                input.Month,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out month))
        {
            ModelState.AddModelError(
                "Month",
                "Please select a valid month."
            );
        }
        else
        {
            month = new DateTime(
                month.Year,
                month.Month,
                1
            );
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryId == input.CategoryId &&
                c.Type == CategoryType.Expense &&
                (c.IsDefault || c.UserId == userId));

        if (category == null)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid expense category."
            );
        }

        if (category != null &&
            month != DateTime.MinValue)
        {
            var duplicateExists = await _context.Budgets
                .AnyAsync(b =>
                    b.BudgetId != id &&
                    b.UserId == userId &&
                    b.CategoryId == input.CategoryId &&
                    b.Month == month);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "A budget already exists for this category and month."
                );
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c =>
                    c.Type == CategoryType.Expense &&
                    (c.IsDefault || c.UserId == userId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(input);
        }

        budget.CategoryId = input.CategoryId;
        budget.Month = month;
        budget.LimitAmount = input.LimitAmount;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


}