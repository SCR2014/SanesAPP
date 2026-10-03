using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sanes.Application.Payments.Models;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Payments.Repositories;

public sealed class PaymentIdempotencyRepository
    : IPaymentIdempotencyRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentIdempotencyRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentIdempotencyClaimResult> ClaimAsync(
        Guid tenantId,
        Guid idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId is required.",
                nameof(tenantId));
        }

        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException(
                "IdempotencyKey is required.",
                nameof(idempotencyKey));
        }

        if (string.IsNullOrWhiteSpace(requestHash) ||
            requestHash.Length != 64)
        {
            throw new ArgumentException(
                "RequestHash must be a SHA-256 hexadecimal value.",
                nameof(requestHash));
        }

        /*
         * El claim forma parte de la misma transacción que
         * posteriormente creará Payment, allocations y receipt.
         *
         * Esto garantiza que si cualquier parte del cobro falla,
         * también desaparece el claim.
         */
        var transaction =
            _dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "Payment idempotency claim requires an active database transaction.");

        var connection =
            _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        var recordId =
            Guid.NewGuid();

        var createdAt =
            DateTime.UtcNow;

        /*
         * Esta operación es el punto de sincronización de la
         * idempotencia.
         *
         * PostgreSQL utiliza el índice UNIQUE:
         *
         *     (TenantId, IdempotencyKey)
         *
         * Si dos transacciones intentan insertar la misma key,
         * una de ellas gana.
         *
         * La otra espera la resolución de la primera transacción:
         *
         * - si la primera hace COMMIT:
         *      ON CONFLICT produce 0 filas insertadas;
         *
         * - si la primera hace ROLLBACK:
         *      la segunda puede insertar y convertirse en owner.
         */
        const string sql = """
            INSERT INTO payment_idempotency_records
                (
                    "Id",
                    "TenantId",
                    "IdempotencyKey",
                    "RequestHash",
                    "PaymentId",
                    "CreatedAt",
                    "CompletedAt"
                )
            VALUES
                (
                    @id,
                    @tenantId,
                    @idempotencyKey,
                    @requestHash,
                    NULL,
                    @createdAt,
                    NULL
                )
            ON CONFLICT
                ("TenantId", "IdempotencyKey")
            DO NOTHING;
            """;

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            sql;

        command.Transaction =
            transaction.GetDbTransaction();

        var idParameter =
            command.CreateParameter();

        idParameter.ParameterName =
            "@id";

        idParameter.Value =
            recordId;

        command.Parameters.Add(
            idParameter);

        var tenantParameter =
            command.CreateParameter();

        tenantParameter.ParameterName =
            "@tenantId";

        tenantParameter.Value =
            tenantId;

        command.Parameters.Add(
            tenantParameter);

        var keyParameter =
            command.CreateParameter();

        keyParameter.ParameterName =
            "@idempotencyKey";

        keyParameter.Value =
            idempotencyKey;

        command.Parameters.Add(
            keyParameter);

        var hashParameter =
            command.CreateParameter();

        hashParameter.ParameterName =
            "@requestHash";

        hashParameter.Value =
            requestHash;

        command.Parameters.Add(
            hashParameter);

        var createdAtParameter =
            command.CreateParameter();

        createdAtParameter.ParameterName =
            "@createdAt";

        createdAtParameter.Value =
            createdAt;

        command.Parameters.Add(
            createdAtParameter);

        var affectedRows =
            await command.ExecuteNonQueryAsync(
                cancellationToken);

        var wasCreated =
            affectedRows == 1;

        /*
         * Recuperamos el record mediante EF para dejarlo tracked.
         *
         * Si acabamos de insertarlo, esta transacción puede verlo.
         *
         * Si perdimos el ON CONFLICT frente a otra transacción,
         * PostgreSQL ya habrá esperado su COMMIT antes de devolver
         * el conflicto, por lo que esta lectura verá el record
         * ganador bajo READ COMMITTED.
         */
        var record =
            await _dbContext.PaymentIdempotencyRecords
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.IdempotencyKey == idempotencyKey,
                    cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException(
                "The payment idempotency record could not be retrieved after the claim operation.");
        }

        return new PaymentIdempotencyClaimResult(
            wasCreated,
            record);
    }
}