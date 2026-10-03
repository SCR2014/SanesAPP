namespace Sanes.Domain.Entities;

public class PaymentIdempotencyRecord
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Tenant Tenant { get; set; } =
        null!;

    /*
     * Identificador generado por el cliente para una
     * operación lógica de cobro.
     *
     * Una misma key dentro de un Tenant representa
     * siempre la misma operación.
     */
    public Guid IdempotencyKey { get; set; }

    /*
     * SHA-256 del contenido semántico de la solicitud.
     *
     * Permite distinguir:
     *
     * misma key + mismo request
     *     => retry legítimo
     *
     * misma key + request diferente
     *     => conflicto
     */
    public string RequestHash { get; set; } =
        string.Empty;

    /*
     * Durante el claim inicial todavía puede no existir
     * Payment.
     *
     * Antes del COMMIT exitoso se completa esta relación
     * dentro de la misma transacción.
     */
    public Guid? PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}