using Sanes.Domain.Entities;

namespace Sanes.Application.Investors.Repositories;

public interface IInvestorRepository
{
    Task AddAsync(
        Investor investor,
        CancellationToken cancellationToken = default);

    Task<List<Investor>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Investor?> GetByIdAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);

    Task<Investor?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdentificationAsync(
        Guid tenantId,
        string identification,
        Guid? excludeInvestorId = null,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}