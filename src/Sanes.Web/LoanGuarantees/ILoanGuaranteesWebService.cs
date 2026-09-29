using Sanes.Application.Loans.DTOs;

namespace Sanes.Web.LoanGuarantees;

public interface ILoanGuaranteesWebService
{
    Task<LoanGuaranteeResponse?> GetGuaranteeAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanGuaranteeResponse?> CreateGuaranteeAsync(
        Guid loanId,
        CreateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanGuaranteeResponse?> UpdateGuaranteeAsync(
        Guid loanId,
        UpdateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default);

    Task<List<LoanGuaranteeAttachmentResponse>?>
        GetAttachmentsAsync(
            Guid loanId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachmentResponse?>
        UploadAttachmentAsync(
            Guid loanId,
            Stream content,
            string fileName,
            string contentType,
            long fileSize,
            string? description,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeDocumentDownload?>
        DownloadAttachmentAsync(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);

    Task<bool> DeleteAttachmentAsync(
        Guid loanId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}