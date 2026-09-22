namespace Sanes.Web.FinancialReports;

public sealed class FinancialReportDownload
{
    public byte[] Content { get; init; } =
        Array.Empty<byte>();

    public string ContentType { get; init; } =
        "application/octet-stream";

    public string FileName { get; init; } =
        "reporte.xlsx";
}