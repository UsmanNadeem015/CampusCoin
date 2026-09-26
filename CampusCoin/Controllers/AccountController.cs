using CampusCoin.Domain.Entities;
using CampusCoin.Infrastructure.Data;
using CampusCoin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CampusCoin.Controllers;

public class AccountController : Controller
{
    private readonly CampusCoinDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly EmailService _emailService;

    public AccountController(
        CampusCoinDbContext context,
        EmailService emailService)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _emailService = emailService;
    }

    

// Registart start

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        bool emailExists = await _context.Users
            .AnyAsync(x => x.Email == input.Email);

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "An account with this email already exists."
            );

            return View(input);
        }

        var user = new User
        {
            Name = input.Name,
            Email = input.Email,
            AcademicYear = input.AcademicYear,
            MonthlyAllowance = input.MonthlyAllowance,
            SavingsGoal = input.SavingsGoal,
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            input.Password
        );

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Login));
    }

    // Register end

    // Login start

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginInput input,
        string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == input.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(input);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been disabled.");
            return View(input);
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            input.Password
        );

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password."
            );

            return View(input);
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                user.Name
            ),

            new Claim(
                ClaimTypes.Email,
                user.Email
            ),

            new Claim(
                ClaimTypes.Role,
                user.IsAdmin ? "Admin" : "Student"
            )
        };

        var identity = new ClaimsIdentity(
            claims,
            "CampusCoinCookie"
        );

        var principal = new ClaimsPrincipal(identity);

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = input.RememberMe
        };

        await HttpContext.SignInAsync(
            "CampusCoinCookie",
            principal,
            authenticationProperties
        );

        if (!string.IsNullOrEmpty(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (user.IsAdmin)
        {
            return RedirectToAction("Index", "Admin");
        }

        return RedirectToAction("Index", "Dashboard");
    }
    // Login End

    // Forgot Password start

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var email = input.Email.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user != null)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            var token = WebEncoders.Base64UrlEncode(tokenBytes);

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token },
                Request.Scheme
            );

            if (!string.IsNullOrEmpty(resetLink))
            {
                await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    resetLink
                );
            }
        }

        TempData["ResetMessage"] =
            "If an account exists with that email, a password reset link has been generated.";

        return RedirectToAction(nameof(ForgotPassword));
    }

    // Forgot Password end


    // Reset Password start

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Invalid password reset link.");
        }

        return View(new ResetPasswordInput
        {
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.PasswordResetToken == input.Token &&
                x.PasswordResetTokenExpiry != null &&
                x.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "This password reset link is invalid or has expired."
            );

            return View(input);
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            input.NewPassword
        );

        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        TempData["ResetMessage"] =
            "Your password has been reset successfully. You can now log in.";

        return RedirectToAction(nameof(Login));
    }

    // Reset Password end

    // Logout start
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CampusCoinCookie");

        return RedirectToAction("Index", "Home");
    }
    // Logout end

    // Access denied start
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    } 
// Access denied end

// Registar input start

    public class RegisterInput
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(50)]
        public string? AcademicYear { get; set; }

        [Range(0, 999999999)]
        public decimal? MonthlyAllowance { get; set; }

        [Range(0, 999999999)]
        public decimal? SavingsGoal { get; set; }
    }

// Register input end

// Login input start
    public class LoginInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
    // Login input end

    public class ResetPasswordInput
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}