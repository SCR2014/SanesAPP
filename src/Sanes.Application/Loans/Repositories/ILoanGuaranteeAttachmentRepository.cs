using Sanes.Domain.Entities;

namespace Sanes.Application.Loans.Repositories;

public interface ILoanGuaranteeAttachmentRepository
{
    Task AddAsync(
        LoanGuaranteeAttachment attachment,
        CancellationToken cancellationToken = default);

    Task<List<LoanGuaranteeAttachment>>
        GetByGuaranteeAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachment?>
        GetByIdAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);

    Task<LoanGuaranteeAttachment?>
        GetByIdForUpdateAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            Guid attachmentId,
            CancellationToken cancellationToken = default);

    Task<int> CountActiveByGuaranteeAsync(
        Guid tenantId,
        Guid loanGuaranteeId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}