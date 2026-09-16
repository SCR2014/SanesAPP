namespace Sanes.Application.LateFees.DTOs;

public class OutstandingLateFeeItem
{
    public Guid LateFeeChargeId { get; set; }

    public Guid LoanId { get; set; }

    public int InstallmentNumber { get; set; }

    public DateTime InstallmentDueDate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public decimal OriginalAmount { get; set; }

    public decimal IncreaseAmount { get; set; }

    public decimal DecreaseAmount { get; set; }

    public decimal WaivedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }
}