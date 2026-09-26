using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class Note
{
    public int NoteId { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? BookmarkId { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public Bookmark? Bookmark { get; set; }
}