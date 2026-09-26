using System.Globalization;
using System.Security.Claims;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class ReportsController : Controller
{
    private readonly CampusCoinDbContext _context;

    public ReportsController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? month)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        DateTime selectedMonth;

        if (!DateTime.TryParseExact(
                month,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out selectedMonth))
        {
            selectedMonth = DateTime.Today;
        }

        var monthStart = new DateTime(
            selectedMonth.Year,
            selectedMonth.Month,
            1
        );

        var nextMonthStart = monthStart.AddMonths(1);

        // Current selected month transactions
        var transactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .Include(t => t.Category)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var totalIncome = transactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount);

        var categorySummary = transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Unknown")
            .Select(g => new CategoryReportItem
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // Six-month report
        var sixMonthStart = monthStart.AddMonths(-5);

        var sixMonthTransactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= sixMonthStart &&
                t.TransactionDate < nextMonthStart)
            .ToListAsync();

        var sixMonthSummary = new List<SixMonthReportItem>();

        for (var i = 0; i < 6; i++)
        {
            var currentMonth = sixMonthStart.AddMonths(i);
            var currentMonthEnd = currentMonth.AddMonths(1);

            var monthTransactions = sixMonthTransactions
                .Where(t =>
                    t.TransactionDate >= currentMonth &&
                    t.TransactionDate < currentMonthEnd)
                .ToList();

            sixMonthSummary.Add(new SixMonthReportItem
            {
                Month = currentMonth,
                Income = monthTransactions
                    .Where(t => t.TransactionType == TransactionType.Income)
                    .Sum(t => t.Amount),
                Expenses = monthTransactions
                    .Where(t => t.TransactionType == TransactionType.Expense)
                    .Sum(t => t.Amount)
            });
        }

        // Daily report for selected month
        var dailySummary = new List<DailyReportItem>();

        var daysInMonth = DateTime.DaysInMonth(
            monthStart.Year,
            monthStart.Month
        );

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(
                monthStart.Year,
                monthStart.Month,
                day
            );

            var nextDate = date.AddDays(1);

            var dayTransactions = transactions
                .Where(t =>
                    t.TransactionDate >= date &&
                    t.TransactionDate < nextDate)
                .ToList();

            dailySummary.Add(new DailyReportItem
            {
                Date = date,
                Income = dayTransactions
                    .Where(t => t.TransactionType == TransactionType.Income)
                    .Sum(t => t.Amount),
                Expenses = dayTransactions
                    .Where(t => t.TransactionType == TransactionType.Expense)
                    .Sum(t => t.Amount)
            });
        }

        // Weekly report for selected month
        var weeklySummary = transactions
            .GroupBy(t =>
            {
                var date = t.TransactionDate.Date;
                var daysFromMonday =
                    ((int)date.DayOfWeek + 6) % 7;

                return date.AddDays(-daysFromMonday);
            })
            .Select(g => new WeeklyReportItem
            {
                WeekStart = g.Key,
                WeekEnd = g.Key.AddDays(6),
                Income = g
                    .Where(t => t.TransactionType == TransactionType.Income)
                    .Sum(t => t.Amount),
                Expenses = g
                    .Where(t => t.TransactionType == TransactionType.Expense)
                    .Sum(t => t.Amount)
            })
            .OrderBy(x => x.WeekStart)
            .ToList();

        ViewBag.SelectedMonth = monthStart.ToString("yyyy-MM");
        ViewBag.MonthDisplay = monthStart.ToString("MMMM yyyy");

        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.Balance = totalIncome - totalExpenses;

        ViewBag.CategorySummary = categorySummary;
        ViewBag.Transactions = transactions;
        ViewBag.SixMonthSummary = sixMonthSummary;
        ViewBag.DailySummary = dailySummary;
        ViewBag.WeeklySummary = weeklySummary;

        return View();
    }

    public class CategoryReportItem
    {
        public string CategoryName { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }

    public class SixMonthReportItem
    {
        public DateTime Month { get; set; }

        public decimal Income { get; set; }

        public decimal Expenses { get; set; }
    }

    public class DailyReportItem
    {
        public DateTime Date { get; set; }

        public decimal Income { get; set; }

        public decimal Expenses { get; set; }
    }

    public class WeeklyReportItem
    {
        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public decimal Income { get; set; }

        public decimal Expenses { get; set; }
    }
}