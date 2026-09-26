using System.Security.Claims;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class NotificationsController : Controller
{
    private readonly CampusCoinDbContext _context;

    public NotificationsController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.UserId == userId);

        if (notification == null)
        {
            return NotFound();
        }

        notification.IsRead = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}