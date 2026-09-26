using CampusCoin.Domain.Entities;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class ProfileController : Controller
{
    private readonly CampusCoinDbContext _context;

    public ProfileController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    string Name,
    string? AcademicYear,
    decimal? MonthlyAllowance,
    decimal? SavingsGoal)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(
                "Name",
                "Name is required."
            );
        }

        if (Name.Length > 100)
        {
            ModelState.AddModelError(
                "Name",
                "Name cannot exceed 100 characters."
            );
        }

        if (MonthlyAllowance < 0)
        {
            ModelState.AddModelError(
                "MonthlyAllowance",
                "Monthly allowance cannot be negative."
            );
        }

        if (SavingsGoal < 0)
        {
            ModelState.AddModelError(
                "SavingsGoal",
                "Savings goal cannot be negative."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        user.Name = Name.Trim();
        user.AcademicYear = AcademicYear;
        user.MonthlyAllowance = MonthlyAllowance;
        user.SavingsGoal = SavingsGoal;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}

