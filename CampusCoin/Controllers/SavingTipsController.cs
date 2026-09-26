using System.Security.Claims;
using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class SavingTipsController : Controller
{
    private readonly CampusCoinDbContext _context;

    public SavingTipsController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        await GenerateSavingTipsAsync(userId);

        var tips = await _context.SavingTips
            .Where(t =>
                t.UserId == userId &&
                !t.IsDismissed)
            .Include(t => t.Category)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.PotentialSaving)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        var tipTemplates = await _context.TipTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.TipTemplates = tipTemplates;

        return View(tips);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePin(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var tip = await _context.SavingTips
            .FirstOrDefaultAsync(t =>
                t.SavingTipId == id &&
                t.UserId == userId);

        if (tip == null)
        {
            return NotFound();
        }

        tip.IsPinned = !tip.IsPinned;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var tip = await _context.SavingTips
            .FirstOrDefaultAsync(t =>
                t.SavingTipId == id &&
                t.UserId == userId);

        if (tip == null)
        {
            return NotFound();
        }

        tip.IsDismissed = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task GenerateSavingTipsAsync(int userId)
    {
        var currentMonth = new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1
        );

        var nextMonth = currentMonth.AddMonths(1);

        var historyStart = currentMonth.AddMonths(-3);

        var transactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= historyStart &&
                t.TransactionDate < nextMonth)
            .ToListAsync();

        var budgets = await _context.Budgets
            .Where(b =>
                b.UserId == userId &&
                b.Month == currentMonth)
            .ToListAsync();

        var categoryIds = transactions
            .Select(t => t.CategoryId)
            .Distinct()
            .ToList();

        foreach (var categoryId in categoryIds)
        {
            var currentSpend = transactions
                .Where(t =>
                    t.CategoryId == categoryId &&
                    t.TransactionDate >= currentMonth &&
                    t.TransactionDate < nextMonth)
                .Sum(t => t.Amount);

            var historicalTransactions = transactions
                .Where(t =>
                    t.CategoryId == categoryId &&
                    t.TransactionDate < currentMonth)
                .ToList();

            var historicalMonths = historicalTransactions
                .GroupBy(t => new
                {
                    t.TransactionDate.Year,
                    t.TransactionDate.Month
                })
                .Select(g => g.Sum(t => t.Amount))
                .ToList();

            if (currentSpend <= 0 || historicalMonths.Count == 0)
            {
                continue;
            }

            var historicalAverage =
                historicalMonths.Average();

            var budget = budgets.FirstOrDefault(
                b => b.CategoryId == categoryId
            );

            var usagePercentage = budget != null &&
                                  budget.LimitAmount > 0
                ? (currentSpend / budget.LimitAmount) * 100m
                : 0m;

            var spendingIncrease =
                historicalAverage > 0
                    ? ((currentSpend - historicalAverage)
                       / historicalAverage) * 100m
                    : 0m;

            if (spendingIncrease >= 20m ||
                usagePercentage >= 80m)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.CategoryId == categoryId);

                var categoryName =
                    category?.Name ?? "your category";

                var potentialSaving =
                    Math.Max(
                        0m,
                        currentSpend - historicalAverage
                    );

                var title =
                    $"Reduce {categoryName} spending";

                var description =
                    spendingIncrease >= 20m
                        ? $"Your {categoryName} spending is " +
                          $"{spendingIncrease:N0}% above your " +
                          $"recent average. Consider reducing " +
                          $"this category this month."
                        : $"Your {categoryName} spending has " +
                          $"reached {usagePercentage:N0}% of your " +
                          $"monthly budget. Consider slowing down " +
                          $"spending in this category.";

                var alreadyExists = await _context.SavingTips
                    .AnyAsync(t =>
                        t.UserId == userId &&
                        t.CategoryId == categoryId &&
                        t.Title == title &&
                        t.CreatedAt >= currentMonth);

                if (!alreadyExists)
                {
                    _context.SavingTips.Add(new SavingTip
                    {
                        UserId = userId,
                        CategoryId = categoryId,
                        Title = title,
                        Description = description,
                        PotentialSaving = potentialSaving,
                        IsPinned = false,
                        IsDismissed = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}