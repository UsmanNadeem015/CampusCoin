using System.Security.Claims;
using CampusCoin.Domain.Entities;
using CampusCoin.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Controllers;

[Authorize(Roles = "Student")]
public class NotesController : Controller
{
    private readonly CampusCoinDbContext _context;

    public NotesController(CampusCoinDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var notes = await _context.Notes
            .Where(n => n.UserId == userId)
            .Include(n => n.Bookmark)
            .OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
            .ToListAsync();

        return View(notes);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? bookmarkId)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        if (bookmarkId.HasValue)
        {
            var bookmarkExists = await _context.Bookmarks
                .AnyAsync(b =>
                    b.BookmarkId == bookmarkId.Value &&
                    b.UserId == userId);

            if (!bookmarkExists)
            {
                bookmarkId = null;
            }
        }

        return View(new Note
        {
            BookmarkId = bookmarkId,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Note input)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return Unauthorized();
        }

        // Set server-controlled values
        input.UserId = userId;
        input.User = user;
        input.CreatedAt = DateTime.UtcNow;

        // User is set by the server, not the form
        ModelState.Remove(nameof(Note.User));

        if (input.BookmarkId.HasValue)
        {
            var bookmarkExists = await _context.Bookmarks
                .AnyAsync(b =>
                    b.BookmarkId == input.BookmarkId.Value &&
                    b.UserId == userId);

            if (!bookmarkExists)
            {
                ModelState.AddModelError(
                    "BookmarkId",
                    "Invalid bookmark."
                );
            }
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        _context.Notes.Add(input);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var note = await _context.Notes
            .FirstOrDefaultAsync(n =>
                n.NoteId == id &&
                n.UserId == userId);

        if (note == null)
        {
            return NotFound();
        }

        return View(note);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Note input)
    {
        if (id != input.NoteId)
        {
            return BadRequest();
        }

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var note = await _context.Notes
            .FirstOrDefaultAsync(n =>
                n.NoteId == id &&
                n.UserId == userId);

        if (note == null)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(Note.User));
        ModelState.Remove(nameof(Note.Bookmark));

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        note.Title = input.Title;
        note.Content = input.Content;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var note = await _context.Notes
            .FirstOrDefaultAsync(n =>
                n.NoteId == id &&
                n.UserId == userId);

        if (note == null)
        {
            return NotFound();
        }

        _context.Notes.Remove(note);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}