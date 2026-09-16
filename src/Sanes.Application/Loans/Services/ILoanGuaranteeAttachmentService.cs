using Sanes.Application.Loans.DTOs;

namespace Sanes.Application.Loans.Services;

public interface ILoanGuaranteeAttachmentService
{
    Task<List<LoanGuaranteeAttachmentResponse>?>
        GetAllAsync(
            Guid tenantId,
            Guid loanId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachmentResponse?>
        GetByIdAsync(
            Guid tenantId,
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachmentContentResponse?>
        OpenContentAsync(
            Guid tenantId,
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachmentResponse?>
        UploadAsync(
            Guid tenantId,
            Guid appUserId,
            Guid loanId,
            string originalFileName,
            string contentType,
            long fileSize,
            Stream content,
            string? description,
            CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid tenantId,
        Guid appUserId,
        Guid loanId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}