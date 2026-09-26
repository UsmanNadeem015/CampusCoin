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

        var category = new Category
        {
            Name = input.Name,
            Type = input.Type,
            IsDefault = false,
            UserId = userId
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public class CategoryInput
    {
        [Required]
        [StringLength(100)]
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
    public async Task<IActionResult> Edit(int id, CategoryInput input)
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
    
            category.Name = input.Name;
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