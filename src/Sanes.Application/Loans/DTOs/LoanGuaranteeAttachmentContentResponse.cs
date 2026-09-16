namespace Sanes.Application.Loans.DTOs;

public class LoanGuaranteeAttachmentContentResponse
{
    public Stream Content { get; set; }
        = Stream.Null;

    public string FileName { get; set; }
        = string.Empty;

    public string ContentType { get; set; }
        = string.Empty;

    public long FileSize { get; set; }
}