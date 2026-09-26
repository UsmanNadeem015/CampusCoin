using System.Security.Claims;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class BookmarksController : Controller
{
    private readonly CampusCoinDbContext _context;

    public BookmarksController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var bookmarks = await _context.Bookmarks
            .Where(b => b.UserId == userId)
            .Include(b => b.SavingTip)
            .Include(b => b.Insight)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookmarks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSavingTip(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var existing = await _context.Bookmarks
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.SavingTipId == id);

        if (existing != null)
        {
            _context.Bookmarks.Remove(existing);
        }
        else
        {
            var tip = await _context.SavingTips
                .FirstOrDefaultAsync(t =>
                    t.SavingTipId == id &&
                    t.UserId == userId);

            if (tip == null)
            {
                return NotFound();
            }

            _context.Bookmarks.Add(new Domain.Entities.Bookmark
            {
                UserId = userId,
                SavingTipId = id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Index",
            "SavingTips"
        );
    }
}