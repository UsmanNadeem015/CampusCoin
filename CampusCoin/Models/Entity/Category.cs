using System.ComponentModel.DataAnnotations;
using CampusCoin.Domain.Enums;

namespace CampusCoin.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CategoryType Type { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public ICollection<SavingTip> SavingTips { get; set; } = new List<SavingTip>();
}