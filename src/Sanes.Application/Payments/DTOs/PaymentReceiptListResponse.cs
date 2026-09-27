namespace Sanes.Application.Payments.DTOs;

public class PaymentReceiptListResponse
{
    public List<PaymentReceiptResponse> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}