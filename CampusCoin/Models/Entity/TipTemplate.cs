using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class TipTemplate
{
    public int TipTemplateId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}