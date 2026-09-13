using Sanes.Domain.Enums;

namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionPaymentResponse
{
    public Guid PaymentId { get; set; }

    public DateTime PaymentDate { get; set; }

    public Guid AppUserId { get; set; }

    public string CollectorName { get; set; } = string.Empty;

    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string ClientPhone { get; set; } = string.Empty;

    public Guid LoanId { get; set; }

    public decimal Amount { get; set; }

    public PaymentType PaymentType { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public DateTime? NextPaymentDate { get; set; }

    public string? Notes { get; set; }
}