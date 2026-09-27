using CampusCoin.Domain.Entities;
using CampusCoin.Domain.Enums;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static CampusCoin.Controllers.AdminController;


namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class CategoriesController : Controller
{
    private readonly CampusCoinDbContext _context;

    public CategoriesController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var categories = await _context.Categories
            .Where(c => c.IsDefault || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var name = input.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                "Name",
                "Category name cannot be empty."
            );

            return View(input);
        }

        var exists = await _context.Categories
            .AnyAsync(c =>
                c.Type == input.Type &&
                c.Name == name &&
                (c.IsDefault || c.UserId == userId)
            );

        if (exists)
        {
            ModelState.AddModelError(
                "Name",
                "A category with this name and type already exists."
            );

            return View(input);
        }

        var category = new Category
        {
            Name = name,
            Type = input.Type,
            IsDefault = false,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    public class CategoryInput
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public CategoryType Type { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryId == id &&
                c.UserId == userId &&
                !c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        var input = new CategoryInput
        {
            Name = category.Name,
            Type = category.Type
        };

        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int id,
    CategoryInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryId == id &&
                c.UserId == userId &&
                !c.IsDefault);

        if (category == null)
        {
            return NotFound();
        }

        var name = input.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                "Name",
                "Category name cannot be empty."
            );

            return View(input);
        }

        var exists = await _context.Categories
            .AnyAsync(c =>
                c.CategoryId != id &&
                c.Type == input.Type &&
                c.Name == name &&
                (c.IsDefault || c.UserId == userId)
            );

        if (exists)
        {
            ModelState.AddModelError(
                "Name",
                "A category with this name and type already exists."
            );

            return View(input);
        }

        category.Name = name;
        category.Type = input.Type;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );
    
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id &&
                    c.UserId == userId &&
                    !c.IsDefault);
    
            if (category == null)
            {
                return NotFound();
            }
    
            return View(category);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );
    
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id &&
                    c.UserId == userId &&
                    !c.IsDefault);
    
            if (category == null)
            {
                return NotFound();
            }
    
            var hasTransactions = await _context.Transactions
                .AnyAsync(t =>
                    t.CategoryId == id &&
                    t.UserId == userId);
    
            if (hasTransactions)
            {
                TempData["Error"] =
                    "This category cannot be deleted because it is being used by transactions.";
    
                return RedirectToAction(nameof(Index));
            }
    
            _context.Categories.Remove(category);
    
            await _context.SaveChangesAsync();
    
            return RedirectToAction(nameof(Index));
        }





    }