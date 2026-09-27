using System.Globalization;
using System.Security.Claims;
using CampusCoin.Domain.Entities;
using CampusCoin.Infrastructure.Data;
using CampusCoin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class AIInsightsController : Controller
{
    private readonly CampusCoinDbContext _context;
    private readonly AIInsightsService _aiInsightsService;

    public AIInsightsController(
        CampusCoinDbContext context,
        AIInsightsService aiInsightsService)
    {
        _context = context;
        _aiInsightsService = aiInsightsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? month,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

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
            1);

        var insight = await _context.Insights
            .Where(i =>
                i.UserId == userId &&
                i.Month == monthStart)
            .OrderByDescending(i => i.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var history = await _context.Insights
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Month)
            .ThenByDescending(i => i.GeneratedAt)
            .ToListAsync(cancellationToken);

        ViewBag.SelectedMonth =
            monthStart.ToString("yyyy-MM");

        ViewBag.MonthDisplay =
            monthStart.ToString("MMMM yyyy");

        ViewBag.History = history;

        return View(insight);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        string? month,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

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
            1);

        // Check whether an insight already exists.
        var existingInsight = await _context.Insights
            .Where(i =>
                i.UserId == userId &&
                i.Month == monthStart)
            .OrderByDescending(i => i.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInsight != null)
        {
            return RedirectToAction(
                nameof(Index),
                new
                {
                    month = monthStart.ToString("yyyy-MM")
                });
        }

        var generatedInsight =
            await _aiInsightsService.GenerateMonthlyInsightAsync(
                userId,
                monthStart,
                cancellationToken);

        if (generatedInsight == null)
        {
            TempData["AIInsightError"] =
                "The monthly insight could not be generated. " +
                "Please try again.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    month = monthStart.ToString("yyyy-MM")
                });
        }

        var insight = new Insight
        {
            UserId = userId,
            Month = monthStart,
            SummaryText = generatedInsight.SummaryText,
            TipText = generatedInsight.TipText,
            GeneratedAt = DateTime.UtcNow
        };

        _context.Insights.Add(insight);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["AIInsightSuccess"] =
            $"AI spending insight generated for " +
            $"{monthStart:MMMM yyyy}.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                month = monthStart.ToString("yyyy-MM")
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(
        string? month,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

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
            1);

        // Delete the existing insight for this student/month.
        var existingInsight = await _context.Insights
            .Where(i =>
                i.UserId == userId &&
                i.Month == monthStart)
            .OrderByDescending(i => i.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInsight != null)
        {
            _context.Insights.Remove(existingInsight);

            await _context.SaveChangesAsync(cancellationToken);
        }

        var generatedInsight =
            await _aiInsightsService.GenerateMonthlyInsightAsync(
                userId,
                monthStart,
                cancellationToken);

        if (generatedInsight == null)
        {
            TempData["AIInsightError"] =
                "The monthly insight could not be regenerated. " +
                "Please try again.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    month = monthStart.ToString("yyyy-MM")
                });
        }

        var insight = new Insight
        {
            UserId = userId,
            Month = monthStart,
            SummaryText = generatedInsight.SummaryText,
            TipText = generatedInsight.TipText,
            GeneratedAt = DateTime.UtcNow
        };

        _context.Insights.Add(insight);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["AIInsightSuccess"] =
            $"AI spending insight regenerated for " +
            $"{monthStart:MMMM yyyy}.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                month = monthStart.ToString("yyyy-MM")
            });
    }
}