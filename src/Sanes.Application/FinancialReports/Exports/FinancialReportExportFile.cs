namespace Sanes.Application.FinancialReports.Exports;

public class FinancialReportExportFile
{
    public byte[] Content { get; set; } =
        Array.Empty<byte>();

    public string ContentType { get; set; } =
        string.Empty;

    public string FileName { get; set; } =
        string.Empty;
}