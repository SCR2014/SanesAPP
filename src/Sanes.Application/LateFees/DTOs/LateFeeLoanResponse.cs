namespace Sanes.Application.LateFees.DTOs;

public class LateFeeLoanResponse
{
    public Guid LoanId { get; set; }

    public decimal TotalOriginalCharges { get; set; }

    public decimal TotalAdjustedCharges { get; set; }

    public decimal TotalPaidToLateFees { get; set; }

    public decimal LateFeeBalance { get; set; }

    public List<LateFeeChargeResponse> Charges { get; set; }
        = new();
}