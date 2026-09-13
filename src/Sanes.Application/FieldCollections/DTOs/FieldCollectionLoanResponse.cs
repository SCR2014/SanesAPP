namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionLoanResponse
{
    public Guid LoanId { get; set; }

    public decimal Balance { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public decimal OverdueAmount { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public bool IsOverdue { get; set; }
}