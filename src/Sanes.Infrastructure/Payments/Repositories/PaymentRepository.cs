using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;


namespace Sanes.Infrastructure.Payments.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(
            payment,
            cancellationToken);
    }

    public async Task<List<Payment>> GetAllByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Reversal)
                .ThenInclude(x => x!.ReversedByAppUser)
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId)
            .OrderBy(x => x.PaymentDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Reversal)
                .ThenInclude(x => x!.ReversedByAppUser)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == paymentId,
                cancellationToken);
    }
    public async Task<Payment?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        /*
        * Igual que Loan.GetByIdForUpdateAsync, este método
        * representa una lectura destinada a modificar estado
        * financiero y requiere una transacción activa.
        *
        * El SELECT ... FOR UPDATE serializa operaciones que
        * intenten modificar/reversar el mismo Payment.
        */
        var transaction =
            _dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "GetByIdForUpdateAsync requires an active database transaction.");

        var connection =
            _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT "Id"
            FROM payments
            WHERE
                "TenantId" = @tenantId
                AND "Id" = @paymentId
            FOR UPDATE;
            """;

        command.Transaction =
            transaction.GetDbTransaction();

        var tenantParameter =
            command.CreateParameter();

        tenantParameter.ParameterName =
            "@tenantId";

        tenantParameter.Value =
            tenantId;

        command.Parameters.Add(
            tenantParameter);

        var paymentParameter =
            command.CreateParameter();

        paymentParameter.ParameterName =
            "@paymentId";

        paymentParameter.Value =
            paymentId;

        command.Parameters.Add(
            paymentParameter);

        var lockedPaymentId =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (lockedPaymentId is null ||
            lockedPaymentId == DBNull.Value)
        {
            return null;
        }

        /*
        * El lock permanece activo hasta COMMIT/ROLLBACK.
        * Aquí recuperamos la entidad tracked y las relaciones
        * requeridas por PaymentReversalService.
        */
        return await _dbContext.Payments
            .Include(x => x.Reversal)
            .Include(x => x.EarlySettlement)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == paymentId,
                cancellationToken);
    }

    public async Task<Payment?> GetLatestEffectiveByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId &&
                x.Reversal == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<decimal> GetTotalPaidAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId &&
                x.Reversal == null)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    public async Task<Dictionary<Guid, decimal>>
        GetTotalPaidByLoansAsync(
            Guid tenantId,
            IEnumerable<Guid> loanIds,
            CancellationToken cancellationToken = default)
    {
        var ids = loanIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                ids.Contains(x.LoanId) &&
                x.Reversal == null)
            .GroupBy(x => x.LoanId)
            .Select(group => new
            {
                LoanId = group.Key,
                TotalPaid = group.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.LoanId,
                x => x.TotalPaid,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}