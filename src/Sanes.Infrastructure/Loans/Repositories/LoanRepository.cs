using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Loans.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly SanesDbContext _dbContext;

    public LoanRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Loan loan,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Loans.AddAsync(
            loan,
            cancellationToken);
    }

    public async Task AddGuaranteeAsync(
        LoanGuarantee guarantee,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.LoanGuarantees.AddAsync(
            guarantee,
            cancellationToken);
    }

    public async Task<List<Loan>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .AsNoTracking()
            .Include(x => x.Investor)
            .Include(x => x.Client)
            .Include(x => x.Guarantee)
            .Where(x =>
                x.TenantId == tenantId)
            .OrderByDescending(x =>
                x.CreatedAt)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<Loan?> GetByIdAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .AsNoTracking()
            .Include(x => x.Investor)
            .Include(x => x.Client)
            .Include(x => x.Guarantee)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == loanId,
                cancellationToken);
    }

    public async Task<Loan?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        /*
        * Históricamente este método también se usa como una lectura
        * tracked por operaciones no transaccionales.
        *
        * Para mantener compatibilidad:
        *
        * - si existe una transacción activa, adquirimos FOR UPDATE;
        * - si no existe, conservamos el comportamiento tracked
        *   anterior.
        *
        * Las operaciones financieras críticas siempre entran aquí
        * dentro de ITransactionRunner.
        */
        var transaction =
            _dbContext.Database.CurrentTransaction;

        if (transaction is not null)
        {
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
                FROM loans
                WHERE
                    "TenantId" = @tenantId
                    AND "Id" = @loanId
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

            var loanParameter =
                command.CreateParameter();

            loanParameter.ParameterName =
                "@loanId";

            loanParameter.Value =
                loanId;

            command.Parameters.Add(
                loanParameter);

            var lockedLoanId =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (lockedLoanId is null ||
                lockedLoanId == DBNull.Value)
            {
                return null;
            }
        }

        return await _dbContext.Loans
            .Include(x => x.Investor)
            .Include(x => x.Client)
            .Include(x => x.Guarantee)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == loanId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<List<Loan>> GetActiveByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .AsNoTracking()
            .Include(x => x.Client)
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == LoanStatus.Active)
            .OrderBy(x => x.NextPaymentDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(
                cancellationToken);
    }
}