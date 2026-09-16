using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.DTOs;

public class LateFeeAdjustmentRequest
{
    public LateFeeAdjustmentType AdjustmentType { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}