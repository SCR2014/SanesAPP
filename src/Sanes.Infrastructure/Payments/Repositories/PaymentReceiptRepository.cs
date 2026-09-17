using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Payments.Repositories;

public class PaymentReceiptRepository
    : IPaymentReceiptRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentReceiptRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PaymentReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentReceipts.AddAsync(
            receipt,
            cancellationToken);
    }

    public async Task<PaymentReceipt?> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.PaymentId == paymentId,
                cancellationToken);
    }

    public async Task<PaymentReceipt?> GetByIdAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == receiptId,
                cancellationToken);
    }

    public async Task<PaymentReceipt?> GetByNumberAsync(
        Guid tenantId,
        string receiptNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.ReceiptNumber == receiptNumber,
                cancellationToken);
    }

    public async Task<int> GetNextSequenceNumberAsync(
        Guid tenantId,
        int year,
        CancellationToken cancellationToken = default)
    {
        /*
         * El INSERT ... ON CONFLICT ... RETURNING se ejecuta
         * como una única operación atómica en PostgreSQL.
         *
         * Dos pagos concurrentes nunca pueden obtener
         * el mismo LastNumber.
         */
        const string sql = """
            INSERT INTO payment_receipt_sequences
                ("TenantId", "Year", "LastNumber", "UpdatedAt")
            VALUES
                (@tenantId, @year, 1, @updatedAt)
            ON CONFLICT ("TenantId", "Year")
            DO UPDATE SET
                "LastNumber" =
                    payment_receipt_sequences."LastNumber" + 1,
                "UpdatedAt" =
                    EXCLUDED."UpdatedAt"
            RETURNING "LastNumber";
            """;

        var connection =
            _dbContext.Database.GetDbConnection();

        var transaction =
            _dbContext.Database.CurrentTransaction;

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        try
        {
            await using var command =
                connection.CreateCommand();

            command.CommandText = sql;

            if (transaction is not null)
            {
                command.Transaction =
                    transaction.GetDbTransaction();
            }

            var tenantParameter =
                command.CreateParameter();

            tenantParameter.ParameterName =
                "@tenantId";

            tenantParameter.Value =
                tenantId;

            command.Parameters.Add(
                tenantParameter);

            var yearParameter =
                command.CreateParameter();

            yearParameter.ParameterName =
                "@year";

            yearParameter.Value =
                year;

            command.Parameters.Add(
                yearParameter);

            var updatedAtParameter =
                command.CreateParameter();

            updatedAtParameter.ParameterName =
                "@updatedAt";

            updatedAtParameter.Value =
                DateTime.UtcNow;

            command.Parameters.Add(
                updatedAtParameter);

            var result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (result is null ||
                result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Could not generate the next payment receipt sequence number.");
            }

            return Convert.ToInt32(
                result,
                CultureInfo.InvariantCulture);
        }
        finally
        {
            /*
             * Si la conexión pertenece a una transacción
             * activa de EF, no debemos cerrarla aquí.
             */
            if (
                shouldCloseConnection &&
                transaction is null)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}