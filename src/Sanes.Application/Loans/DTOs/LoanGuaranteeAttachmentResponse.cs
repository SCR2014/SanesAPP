namespace Sanes.Application.Loans.DTOs;

public class LoanGuaranteeAttachmentResponse
{
    public Guid Id { get; set; }

    public Guid LoanGuaranteeId { get; set; }

    public Guid UploadedByAppUserId { get; set; }

    public string OriginalFileName { get; set; }
        = string.Empty;

    public string ContentType { get; set; }
        = string.Empty;

    public long FileSize { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}