using Microsoft.EntityFrameworkCore;
using Sanes.Application.Common.Persistence;

namespace Sanes.Infrastructure.Persistence;

public class EfTransactionRunner
    : ITransactionRunner
{
    private readonly SanesDbContext _dbContext;

    public EfTransactionRunner(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        /*
         * Si ya existe una transacción en este DbContext,
         * reutilizamos esa transacción.
         *
         * Esto es necesario para escenarios como
         * EarlySettlement -> PaymentService.
         *
         * El servicio exterior conserva la responsabilidad
         * de COMMIT / ROLLBACK.
         */
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(
                cancellationToken);
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var result =
                await operation(
                    cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}