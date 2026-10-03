namespace Sanes.Application.Payments.Models;

public sealed record PaymentIdempotencyContext(
    Guid Key,
    string RequestHash);