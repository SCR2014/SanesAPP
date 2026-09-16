using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.DTOs;

public class LateFeeAdjustmentResponse
{
    public Guid Id { get; set; }

    public Guid LateFeeChargeId { get; set; }

    public Guid AppUserId { get; set; }

    public LateFeeAdjustmentType AdjustmentType { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}