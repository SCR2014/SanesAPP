using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.DTOs;

public class LateFeeChargeResponse
{
    public Guid Id { get; set; }

    public Guid LoanId { get; set; }

    public int InstallmentNumber { get; set; }

    public DateTime InstallmentDueDate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public LateFeeCalculationType CalculationType { get; set; }

    public decimal OriginalAmount { get; set; }

    public decimal IncreaseAmount { get; set; }

    public decimal DecreaseAmount { get; set; }

    public decimal WaivedAmount { get; set; }

    public decimal AdjustedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<LateFeeAdjustmentResponse> Adjustments { get; set; }
        = new();
}