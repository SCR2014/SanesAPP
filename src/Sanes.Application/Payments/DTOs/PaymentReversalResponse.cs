using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.DTOs;

public class PaymentReversalResponse
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Guid LoanId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public Guid ReversedByAppUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}