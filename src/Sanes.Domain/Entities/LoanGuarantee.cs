using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class LoanGuarantee
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public LoanGuaranteeType Type { get; set; }

    /*
     * Número o referencia principal de la garantía.
     *
     * Ejemplos:
     * - número de identificación
     * - matrícula
     * - placa
     * - referencia del bien
     */
    public string Reference { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } =
        DateTime.UtcNow;
}