namespace Sanes.Application.FinancialReports.Models;

public class FinancialReportInvestorData
{
    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }
}