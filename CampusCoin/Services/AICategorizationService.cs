#pragma warning disable OPENAI001

using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using OpenAI.Responses;

namespace CampusCoin.Services;

public class AICategorizationService
{
    private readonly ResponsesClient _client;

    // Cost-conscious model suited to this kind of focused classification task.
    private const string Model = "gpt-5-mini";

    public AICategorizationService(ResponsesClient client)
    {
        _client = client;
    }

    public async Task<int?> SuggestCategoryAsync(
        string? description,
        TransactionType transactionType,
        IEnumerable<Category> categories,
        CancellationToken cancellationToken = default)
    {
        // Nothing useful to classify.
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Only offer categories that match the transaction type.
        var validCategories = categories
            .Where(c => (int)c.Type == (int)transactionType)
            .ToList();

        if (validCategories.Count == 0)
        {
            return null;
        }

        var categoryList = string.Join(
            Environment.NewLine,
            validCategories.Select(c =>
                $"ID: {c.CategoryId} | " +
                $"Name: {c.Name} | " +
                $"Description: {c.Description ?? "No description provided"}")
        );

        var prompt = $"""
            You are an expense categorization assistant for CampusCoin,
            a student budgeting application.

            Your task is to select the most appropriate category
            for the student's transaction description.

            Transaction type:
            {transactionType}

            Transaction description:
            "{description.Trim()}"

            Available categories:
            {categoryList}

            Rules:
            1. Select exactly ONE category from the available categories.
            2. Use both the category name and category description.
            3. Never invent a category.
            4. Return ONLY the numeric Category ID.
            5. Do not return words, explanations, punctuation, or markdown.
            """;

        try
        {
            var response = await _client.CreateResponseAsync(
                Model,
                prompt,
                cancellationToken: cancellationToken
            );

            var output = response.Value
                .GetOutputText()
                .Trim();

            if (!int.TryParse(output, out var suggestedCategoryId))
            {
                return null;
            }

            // Final safety check:
            // AI can only return a category that actually exists
            // in the list we supplied.
            var isValid = validCategories.Any(
                c => c.CategoryId == suggestedCategoryId
            );

            return isValid
                ? suggestedCategoryId
                : null;
        }
        catch
        {
            // AI failure should never prevent normal transaction creation.
            return null;
        }
    }
}

#pragma warning restore OPENAI001