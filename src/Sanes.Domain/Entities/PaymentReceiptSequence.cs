namespace Sanes.Domain.Entities;

public class PaymentReceiptSequence
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int Year { get; set; }

    public int LastNumber { get; set; }

    public DateTime UpdatedAt { get; set; } =
        DateTime.UtcNow;
}