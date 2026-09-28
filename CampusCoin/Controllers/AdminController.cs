using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace CampusCoin.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly CampusCoinDbContext _context;

    public AdminController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();

        var activeUsers = await _context.Users
            .CountAsync(u => u.IsActive);

        var totalTransactions = await _context.Transactions.CountAsync();

        var totalCategories = await _context.Categories.CountAsync();

        var totalBudgets = await _context.Budgets.CountAsync();

        var totalInsights = await _context.Insights.CountAsync();

        var totalSavingTips = await _context.SavingTips.CountAsync();

        var totalTipTemplates = await _context.TipTemplates.CountAsync();

        var mostUsedCategory = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new
            {
                CategoryName = g.Key,
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TransactionCount)
            .FirstOrDefaultAsync();

        ViewBag.TotalUsers = totalUsers;
        ViewBag.ActiveUsers = activeUsers;
        ViewBag.TotalTransactions = totalTransactions;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalBudgets = totalBudgets;
        ViewBag.TotalInsights = totalInsights;
        ViewBag.TotalSavingTips = totalSavingTips;
        ViewBag.TotalTipTemplates = totalTipTemplates;

        ViewBag.MostUsedCategory =
            mostUsedCategory?.CategoryName ?? "No transactions yet";

        ViewBag.MostUsedCategoryCount =
            mostUsedCategory?.TransactionCount ?? 0;

        return View();
    }

    // Users start

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

        return View(users);
    }

    // User status start

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        var currentUserIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        if (currentUserIdClaim != null &&
            int.TryParse(currentUserIdClaim.Value, out int currentUserId) &&
            user.UserId == currentUserId)
        {
            TempData["Error"] = "You cannot disable your own account.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = user.IsActive
            ? "User account enabled successfully."
            : "User account disabled successfully.";

        return RedirectToAction(nameof(Users));
    }

    // User status end
    // User end

    // Categories start

    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .Where(c => c.IsDefault)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    // Create start

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Type))
        {
            ModelState.Remove(nameof(input.Type));
            ModelState.AddModelError(nameof(input.Type), "Please select a category.");
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        if (!Enum.TryParse<CategoryType>(input.Type, out var type))
        {
            ModelState.AddModelError(
                "Type",
                "Please select a valid category type."
            );

            return View(input);
        }

        var exists = await _context.Categories.AnyAsync(c =>
            c.IsDefault &&
            c.Type == type &&
            c.Name == input.Name
        );

        if (exists)
        {
            ModelState.AddModelError(
                "Name",
                "A default category with this name and type already exists."
            );

            return View(input);
        }

        var category = new Category
        {
            Name = input.Name,
            Type = type,
            IsDefault = true,
            UserId = null
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Default category added successfully.";

        return RedirectToAction(nameof(Categories));
    }
    // Create end

    // Edit start

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        var input = new CategoryInput
        {
            Name = category.Name,
            Type = category.Type.ToString()   // enum → string
        };

        ViewBag.CategoryId = category.CategoryId;

        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(
        int id,
        CategoryInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = id;
            return View(input);
        }

        if (!Enum.TryParse<CategoryType>(input.Type, out var type))
        {
            ModelState.AddModelError(
                "Type",
                "Please select a valid category type."
            );

            ViewBag.CategoryId = id;
            return View(input);
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        var exists = await _context.Categories.AnyAsync(c =>
            c.IsDefault &&
            c.CategoryId != id &&
            c.Type == type &&
            c.Name == input.Name
        );

        if (exists)
        {
            ModelState.AddModelError(
                "Name",
                "A default category with this name and type already exists."
            );

            ViewBag.CategoryId = id;
            return View(input);
        }

        category.Name = input.Name;
        category.Type = type;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Default category updated successfully.";

        return RedirectToAction(nameof(Categories));
    }
    // Edit end

    // Delete start

    [HttpGet]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ActionName("DeleteCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategoryConfirmed(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        var usedByTransactions = await _context.Transactions
            .AnyAsync(t => t.CategoryId == id);

        var usedByBudgets = await _context.Budgets
            .AnyAsync(b => b.CategoryId == id);

        if (usedByTransactions || usedByBudgets)
        {
            TempData["Error"] =
                "This category cannot be deleted because it is already being used.";

            return RedirectToAction(nameof(Categories));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Default category deleted successfully.";

        return RedirectToAction(nameof(Categories));
    }
    // Delete end

    // Input class start
    public class CategoryInput
    {
        [Required(ErrorMessage = "Please enter a category name.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        public string? Type { get; set; }
    }
    // Input end

    // Categories end

    // Tip template start
    public class TipTemplateInput
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> TipTemplates()
    {
        var templates = await _context.TipTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(templates);
    }

    [HttpGet]
    public IActionResult CreateTipTemplate()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTipTemplate(
    TipTemplateInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var template = new TipTemplate
        {
            Title = input.Title,
            Description = input.Description,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.TipTemplates.Add(template);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Tip template created successfully.";

        return RedirectToAction(nameof(TipTemplates));
    }

    [HttpGet]
    public async Task<IActionResult> EditTipTemplate(int id)
    {
        var template = await _context.TipTemplates
            .FirstOrDefaultAsync(t => t.TipTemplateId == id);

        if (template == null)
        {
            return NotFound();
        }

        var input = new TipTemplateInput
        {
            Title = template.Title,
            Description = template.Description,
            IsActive = template.IsActive
        };

        ViewBag.TipTemplateId = template.TipTemplateId;

        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTipTemplate(
    int id,
    TipTemplateInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TipTemplateId = id;
            return View(input);
        }

        var template = await _context.TipTemplates
            .FirstOrDefaultAsync(t => t.TipTemplateId == id);

        if (template == null)
        {
            return NotFound();
        }

        template.Title = input.Title;
        template.Description = input.Description;
        template.IsActive = input.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Tip template updated successfully.";

        return RedirectToAction(nameof(TipTemplates));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteTipTemplate(int id)
    {
        var template = await _context.TipTemplates
            .FirstOrDefaultAsync(t => t.TipTemplateId == id);

        if (template == null)
        {
            return NotFound();
        }

        return View(template);
    }

    [HttpPost]
    [ActionName("DeleteTipTemplate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTipTemplateConfirmed(int id)
    {
        var template = await _context.TipTemplates
            .FirstOrDefaultAsync(t => t.TipTemplateId == id);

        if (template == null)
        {
            return NotFound();
        }

        _context.TipTemplates.Remove(template);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Tip template deleted successfully.";

        return RedirectToAction(nameof(TipTemplates));
    }
    // Tip template end

    // Stats start
    [HttpGet]
    public async Task<IActionResult> Statistics()
    {
        var activeUsers = await _context.Users
            .CountAsync(u => u.IsActive);

        var totalTransactions = await _context.Transactions
            .CountAsync();

        var totalTransactionAmount = await _context.Transactions
            .SumAsync(t => t.Amount);

        var mostUsedCategories = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new
            {
                CategoryName = g.Key,
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TransactionCount)
            .Take(5)
            .ToListAsync();

        ViewBag.ActiveUsers = activeUsers;
        ViewBag.TotalTransactions = totalTransactions;
        ViewBag.TotalTransactionAmount = totalTransactionAmount;
        ViewBag.MostUsedCategories = mostUsedCategories;

        return View();
    }
    // Stats end
}