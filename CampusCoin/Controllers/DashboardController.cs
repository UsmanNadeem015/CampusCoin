using System.Security.Claims;
using CampusCoin.Infrastructure.Data;
using CampusCoin.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusCoin.Domain.Entities;

namespace CampusCoin.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly CampusCoinDbContext _context;

    public DashboardController(CampusCoinDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        await ProcessRecurringTransactionsAsync(userId);

        var today = DateTime.Today;

        var monthStart = new DateTime(
            today.Year,
            today.Month,
            1
        );

        var nextMonthStart = monthStart.AddMonths(1);

        // Current month income
        var totalIncome = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == TransactionType.Income &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Current month expenses
        var totalExpenses = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Current balance
        var balance = totalIncome - totalExpenses;

        // Top spending category
        var topCategory = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .Include(t => t.Category)
            .GroupBy(t => t.Category.Name)
            .Select(g => new
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .FirstOrDefaultAsync();

        // Recent transactions
        var recentTransactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.TransactionId)
            .Take(5)
            .ToListAsync();

        ViewBag.UserName = User.Identity?.Name ?? "Student";
        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.Balance = balance;

        ViewBag.TopCategory =
            topCategory?.CategoryName ?? "No expenses yet";

        ViewBag.TopCategoryAmount =
            topCategory?.Amount ?? 0;

        ViewBag.RecentTransactions = recentTransactions;

        var savingTips = await _context.SavingTips
         .Where(t => t.UserId == userId && !t.IsDismissed)
         .Include(t => t.Category)
         .OrderByDescending(t => t.IsPinned)
         .ThenByDescending(t => t.PotentialSaving)
         .ThenByDescending(t => t.CreatedAt)
         .Take(3)
         .ToListAsync();

        ViewBag.SavingTips = savingTips;

        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        var currentBudgets = await _context.Budgets
            .Where(b => b.UserId == userId && b.Month == currentMonth)
            .Include(b => b.Category)
            .ToListAsync();

        var budgetSummary = currentBudgets.Select(b =>
        {
            var actualSpent = _context.Transactions
                .Where(t =>
                    t.UserId == userId &&
                    t.CategoryId == b.CategoryId &&
                    t.TransactionType == TransactionType.Expense &&
                    t.TransactionDate >= currentMonth &&
                    t.TransactionDate < nextMonth)
                .Sum(t => t.Amount);

            return new
            {
                CategoryName = b.Category.Name,
                Budget = b.LimitAmount,
                Actual = actualSpent,
                Remaining = b.LimitAmount - actualSpent
            };
        }).ToList();

        ViewBag.BudgetSummary = budgetSummary;

        return View();
    }

    private async Task ProcessRecurringTransactionsAsync(int userId)
    {
        var today = DateTime.Today;

        var recurringTransactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.IsRecurring &&
                t.NextRecurringDate != null &&
                t.NextRecurringDate <= today)
            .ToListAsync();

        foreach (var recurring in recurringTransactions)
        {
            while (recurring.NextRecurringDate <= today)
            {
                var occurrenceDate = recurring.NextRecurringDate.Value;

                var newTransaction = new Transaction
                {
                    UserId = recurring.UserId,
                    CategoryId = recurring.CategoryId,
                    TransactionType = recurring.TransactionType,
                    Amount = recurring.Amount,
                    Description = recurring.Description,
                    TransactionDate = occurrenceDate,

                    // Generated occurrence is not itself a recurring template
                    IsRecurring = false,
                    NextRecurringDate = null
                };

                _context.Transactions.Add(newTransaction);

                recurring.NextRecurringDate = occurrenceDate.AddMonths(1);
            }
        }

        await _context.SaveChangesAsync();
    }
}