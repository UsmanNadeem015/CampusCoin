using System.ComponentModel.DataAnnotations;
using CampusCoin.Domain.Enums;

namespace CampusCoin.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType TransactionType { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public bool IsRecurring { get; set; }

    public DateTime? NextRecurringDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public Category? Category { get; set; }
}