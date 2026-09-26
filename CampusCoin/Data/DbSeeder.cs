using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CampusCoinDbContext context)
    {
        var defaultCategories = new[]
        {
            new Category
            {
                Name = "Allowance",
                Type = CategoryType.Income,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Part-time Job",
                Type = CategoryType.Income,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Scholarship",
                Type = CategoryType.Income,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Gift",
                Type = CategoryType.Income,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Other Income",
                Type = CategoryType.Income,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Food",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Transport",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Hostel/Rent",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Academics",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Subscriptions",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Entertainment",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },

            new Category
            {
                Name = "Miscellaneous",
                Type = CategoryType.Expense,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var category in defaultCategories)
        {
            bool exists = await context.Categories.AnyAsync(c =>
                c.IsDefault &&
                c.Name == category.Name &&
                c.Type == category.Type);

            if (!exists)
            {
                context.Categories.Add(category);
            }
        }

        await context.SaveChangesAsync();
    }
}