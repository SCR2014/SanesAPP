using Sanes.Domain.Entities;

namespace Sanes.Application.Payments.Models;

public sealed record PaymentIdempotencyClaimResult(
    bool WasCreated,
    PaymentIdempotencyRecord Record);