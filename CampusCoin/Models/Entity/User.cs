using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class User
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    [StringLength(50)]
    public string? AcademicYear { get; set; }

    [Range(0, 999999999)]
    public decimal? MonthlyAllowance { get; set; }

    [Range(0, 999999999)]
    public decimal? SavingsGoal { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public ICollection<Insight> Insights { get; set; } = new List<Insight>();

    public ICollection<SavingTip> SavingTips { get; set; } = new List<SavingTip>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}