using System.ComponentModel.DataAnnotations;
using CampusCoin.Domain.Enums;

namespace CampusCoin.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}