namespace Sanes.Application.Payments.DTOs;

public class PaymentReceiptListRequest
{
    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public string? Search { get; set; }

    public Guid? CollectorId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}