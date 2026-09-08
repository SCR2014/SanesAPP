using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.DTOs;

public class PaymentResponse
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LoanId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}