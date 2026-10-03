namespace Sanes.Application.Payments.Exceptions;

public sealed class PaymentIdempotencyConflictException
    : Exception
{
    public PaymentIdempotencyConflictException(
        string message)
        : base(message)
    {
    }
}