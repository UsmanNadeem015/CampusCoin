using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class Bookmark
{
    public int BookmarkId { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? InsightId { get; set; }

    public int? SavingTipId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Insight? Insight { get; set; }

    public SavingTip? SavingTip { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}