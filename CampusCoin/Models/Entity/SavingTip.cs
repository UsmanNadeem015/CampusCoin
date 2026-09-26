using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class SavingTip
{
    public int SavingTipId { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? CategoryId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 999999999.99)]
    public decimal? PotentialSaving { get; set; }

    public bool IsPinned { get; set; }

    public bool IsDismissed { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Category? Category { get; set; }

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}