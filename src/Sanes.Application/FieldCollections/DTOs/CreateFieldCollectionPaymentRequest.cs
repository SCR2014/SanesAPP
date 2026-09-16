using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.FieldCollections.DTOs;

public class CreateFieldCollectionPaymentRequest
{
    [Required]
    public Guid CollectionRouteId { get; set; }

    [Required]
    public Guid LoanId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public PaymentType PaymentType { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}