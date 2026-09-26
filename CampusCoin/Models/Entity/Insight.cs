using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class Insight
{
    public int InsightId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public DateTime Month { get; set; }

    [Required]
    [StringLength(2000)]
    public string SummaryText { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? TipText { get; set; }

    public DateTime GeneratedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}