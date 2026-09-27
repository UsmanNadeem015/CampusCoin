#pragma warning disable OPENAI001

using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenAI.Responses;

namespace CampusCoin.Services;

public class AIInsightsService
{
    private readonly CampusCoinDbContext _context;
    private readonly ResponsesClient _client;

    private const string Model = "gpt-5-mini";

    public AIInsightsService(
        CampusCoinDbContext context,
        ResponsesClient client)
    {
        _context = context;
        _client = client;
    }

    public async Task<GeneratedInsightResult?> GenerateMonthlyInsightAsync(
        int userId,
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(
            month.Year,
            month.Month,
            1);

        var nextMonthStart = monthStart.AddMonths(1);

        // Current month transactions
        var currentTransactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .Include(t => t.Category)
            .ToListAsync(cancellationToken);

        // Previous month
        var previousMonthStart = monthStart.AddMonths(-1);

        var previousTransactions = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= previousMonthStart &&
                t.TransactionDate < monthStart)
            .Include(t => t.Category)
            .ToListAsync(cancellationToken);

        var currentExpenses = currentTransactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .ToList();

        var previousExpenses = previousTransactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .ToList();

        var currentTotal = currentExpenses.Sum(t => t.Amount);
        var previousTotal = previousExpenses.Sum(t => t.Amount);

        var currentIncome = currentTransactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        var previousIncome = previousTransactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        // Category totals for current month
        var currentCategoryTotals = currentExpenses
            .GroupBy(t => t.Category?.Name ?? "Unknown")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount));

        // Category totals for previous month
        var previousCategoryTotals = previousExpenses
            .GroupBy(t => t.Category?.Name ?? "Unknown")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount));

        // Calculate category growth.
        var categoryGrowth = new List<CategoryGrowth>();

        var categoryNames = currentCategoryTotals.Keys
            .Union(previousCategoryTotals.Keys)
            .Distinct();

        foreach (var categoryName in categoryNames)
        {
            currentCategoryTotals.TryGetValue(
                categoryName,
                out var currentAmount);

            previousCategoryTotals.TryGetValue(
                categoryName,
                out var previousAmount);

            decimal? growthPercentage = null;

            if (previousAmount > 0)
            {
                growthPercentage =
                    ((currentAmount - previousAmount)
                    / previousAmount) * 100m;
            }

            categoryGrowth.Add(new CategoryGrowth
            {
                CategoryName = categoryName,
                CurrentAmount = currentAmount,
                PreviousAmount = previousAmount,
                GrowthPercentage = growthPercentage
            });
        }

        var significantGrowth = categoryGrowth
            .Where(x =>
                x.GrowthPercentage.HasValue &&
                x.GrowthPercentage.Value >= 20m &&
                x.CurrentAmount > x.PreviousAmount)
            .OrderByDescending(x => x.GrowthPercentage)
            .Take(5)
            .ToList();

        var significantReductions = categoryGrowth
            .Where(x =>
                x.GrowthPercentage.HasValue &&
                x.GrowthPercentage.Value <= -20m &&
                x.CurrentAmount < x.PreviousAmount)
            .OrderBy(x => x.GrowthPercentage)
            .Take(5)
            .ToList();

        var overallGrowth = previousTotal > 0
            ? ((currentTotal - previousTotal)
                / previousTotal) * 100m
            : (decimal?)null;

        // Build factual information for the AI.
        var categoryFacts = categoryGrowth
            .OrderByDescending(x => x.CurrentAmount)
            .Take(10)
            .Select(x =>
            {
                var growthText = x.GrowthPercentage.HasValue
                    ? $"{x.GrowthPercentage.Value:N1}%"
                    : "No previous-month comparison";

                return
                    $"- {x.CategoryName}: " +
                    $"current Rs. {x.CurrentAmount:N2}, " +
                    $"previous Rs. {x.PreviousAmount:N2}, " +
                    $"change {growthText}";
            });

        var growthFlags = significantGrowth.Count > 0
            ? string.Join(
                Environment.NewLine,
                significantGrowth.Select(x =>
                    $"- {x.CategoryName} increased by " +
                    $"{x.GrowthPercentage!.Value:N1}% " +
                    $"(Rs. {x.PreviousAmount:N2} → " +
                    $"Rs. {x.CurrentAmount:N2})"))
            : "- No category increased by 20% or more.";

        var reductionFlags = significantReductions.Count > 0
            ? string.Join(
                Environment.NewLine,
                significantReductions.Select(x =>
                    $"- {x.CategoryName} decreased by " +
                    $"{Math.Abs(x.GrowthPercentage!.Value):N1}% " +
                    $"(Rs. {x.PreviousAmount:N2} → " +
                    $"Rs. {x.CurrentAmount:N2})"))
            : "- No category decreased by 20% or more.";

        var overallGrowthText = overallGrowth.HasValue
            ? $"{overallGrowth.Value:N1}%"
            : "No previous-month expense data available.";

        var prompt = $"""
            You are the monthly spending insights assistant
            for CampusCoin, a student budgeting application.

            Analyze the student's monthly spending facts below.

            Current month:
            {monthStart:MMMM yyyy}

            Previous month:
            {previousMonthStart:MMMM yyyy}

            Current month income:
            Rs. {currentIncome:N2}

            Previous month income:
            Rs. {previousIncome:N2}

            Current month expenses:
            Rs. {currentTotal:N2}

            Previous month expenses:
            Rs. {previousTotal:N2}

            Overall expense change:
            {overallGrowthText}

            Category spending:
            {string.Join(Environment.NewLine, categoryFacts)}

            Categories with significant increases:
            {growthFlags}

            Categories with significant decreases:
            {reductionFlags}

            Instructions:

            1. Write a short plain-language summary of the student's
               spending for this month.

            2. Mention notable spending patterns supported by the
               provided facts.

            3. If a category increased significantly, mention the
               category and exact percentage.

            4. Give one practical and realistic budgeting suggestion
               based only on the provided spending information.

            5. Do not invent transactions, amounts, percentages,
               categories, or personal circumstances.

            6. Do not claim certainty about why the student spent
               more or less.

            7. Keep the summary concise, around 2-4 sentences.

            8. Keep the budgeting tip to 1-2 sentences.

            Return exactly this format:

            SUMMARY:
            <summary>

            TIP:
            <actionable tip>
            """;

        try
        {
            var response = await _client.CreateResponseAsync(
                Model,
                prompt,
                cancellationToken: cancellationToken);

            var output = response.Value
                .GetOutputText()
                .Trim();

            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            var summary = ExtractSection(
                output,
                "SUMMARY:",
                "TIP:");

            var tip = ExtractSection(
                output,
                "TIP:",
                null);

            if (string.IsNullOrWhiteSpace(summary))
            {
                return null;
            }

            return new GeneratedInsightResult
            {
                SummaryText = summary,
                TipText = string.IsNullOrWhiteSpace(tip)
                    ? null
                    : tip
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSection(
        string text,
        string startMarker,
        string? endMarker)
    {
        var startIndex = text.IndexOf(
            startMarker,
            StringComparison.OrdinalIgnoreCase);

        if (startIndex < 0)
        {
            return null;
        }

        startIndex += startMarker.Length;

        var remaining = text[startIndex..];

        if (!string.IsNullOrWhiteSpace(endMarker))
        {
            var endIndex = remaining.IndexOf(
                endMarker,
                StringComparison.OrdinalIgnoreCase);

            if (endIndex >= 0)
            {
                remaining = remaining[..endIndex];
            }
        }

        return remaining.Trim();
    }

    private class CategoryGrowth
    {
        public string CategoryName { get; set; } = string.Empty;

        public decimal CurrentAmount { get; set; }

        public decimal PreviousAmount { get; set; }

        public decimal? GrowthPercentage { get; set; }
    }

    public class GeneratedInsightResult
    {
        public string SummaryText { get; set; } = string.Empty;

        public string? TipText { get; set; }
    }
}

#pragma warning restore OPENAI001