namespace Sanes.Web.LoanGuarantees;

public sealed class LoanGuaranteeDocumentDownload
{
    public byte[] Content { get; init; } =
        Array.Empty<byte>();

    public string FileName { get; init; } =
        string.Empty;

    public string ContentType { get; init; } =
        "application/octet-stream";
}