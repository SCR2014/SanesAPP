namespace Sanes.Domain.Entities;

public class LoanGuaranteeAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanGuaranteeId { get; set; }
    public LoanGuarantee LoanGuarantee { get; set; } = null!;

    /*
     * Usuario administrador que cargó el documento.
     */
    public Guid UploadedByAppUserId { get; set; }
    public AppUser UploadedByAppUser { get; set; } = null!;

    /*
     * Nombre original enviado por el cliente.
     *
     * Solo es metadata. Nunca se utilizará
     * directamente como nombre físico.
     */
    public string OriginalFileName { get; set; }
        = string.Empty;

    /*
     * Identificador interno del archivo dentro
     * del storage.
     *
     * Ejemplo:
     * loan-guarantees/{tenantId}/{guaranteeId}/{guid}.pdf
     */
    public string StorageKey { get; set; }
        = string.Empty;

    public string ContentType { get; set; }
        = string.Empty;

    /*
     * Tamaño en bytes.
     */
    public long FileSize { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    /*
     * Soft delete.
     */
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedByAppUserId { get; set; }
    public AppUser? DeletedByAppUser { get; set; }
}