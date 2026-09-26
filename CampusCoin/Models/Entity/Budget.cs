using System.ComponentModel.DataAnnotations;

namespace CampusCoin.Domain.Entities;

public class Budget
{
    public int BudgetId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public DateTime Month { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal LimitAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Category Category { get; set; } = null!;
}