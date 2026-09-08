using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ITenantRepository _tenantRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ILoanRepository loanRepository,
        ITenantRepository tenantRepository)
    {
        _paymentRepository = paymentRepository;
        _loanRepository = loanRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            request.TenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            throw new InvalidOperationException(
                "Tenant not found or inactive.");
        }

        var loan = await _loanRepository.GetByIdForUpdateAsync(
            request.TenantId,
            request.LoanId,
            cancellationToken);

        if (loan is null)
        {
            throw new InvalidOperationException(
                "Loan not found or does not belong to the specified tenant.");
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Payments can only be registered for active loans.");
        }

        var totalAmount =
            loan.InstallmentAmount * loan.TotalInstallments;

        var totalPaidBefore =
            await _paymentRepository.GetTotalPaidAsync(
                request.TenantId,
                request.LoanId,
                cancellationToken);

        var balanceBefore =
            totalAmount - totalPaidBefore;

        if (balanceBefore <= 0)
        {
            throw new InvalidOperationException(
                "This loan has no outstanding balance.");
        }

        if (request.Amount > balanceBefore)
        {
            throw new InvalidOperationException(
                $"Payment amount cannot exceed the outstanding balance of {balanceBefore:0.00}.");
        }

        ValidatePaymentType(
            request.PaymentType,
            request.Amount,
            loan.InstallmentAmount,
            balanceBefore);

        var paymentDate =
            NormalizeUtc(request.PaymentDate);

        var payment = new Payment
        {
            TenantId = request.TenantId,
            LoanId = request.LoanId,
            Amount = request.Amount,
            PaymentDate = paymentDate,
            PaymentType = request.PaymentType,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken);

        var totalPaidAfter =
            totalPaidBefore + request.Amount;

        var balanceAfter =
            totalAmount - totalPaidAfter;

        if (balanceAfter == 0)
        {
            loan.Status = LoanStatus.Paid;
        }
        else
        {
            UpdateNextPaymentDate(
                loan,
                totalPaidBefore,
                totalPaidAfter);
        }

        loan.UpdatedAt = DateTime.UtcNow;

        /*
         * PaymentRepository y LoanRepository utilizan el mismo
         * SanesDbContext dentro del mismo scope de la petición.
         *
         * Por eso un único SaveChangesAsync persiste tanto el Payment
         * nuevo como los cambios realizados al Loan.
         */
        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        return Map(payment);
    }

    public async Task<List<PaymentResponse>> GetAllByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(
            tenantId,
            loanId,
            cancellationToken);

        if (loan is null)
        {
            throw new InvalidOperationException(
                "Loan not found or does not belong to the specified tenant.");
        }

        var payments =
            await _paymentRepository.GetAllByLoanAsync(
                tenantId,
                loanId,
                cancellationToken);

        return payments
            .Select(Map)
            .ToList();
    }

    public async Task<PaymentResponse?> GetByIdAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment =
            await _paymentRepository.GetByIdAsync(
                tenantId,
                paymentId,
                cancellationToken);

        return payment is null
            ? null
            : Map(payment);
    }

    private static void ValidatePaymentType(
        PaymentType paymentType,
        decimal amount,
        decimal installmentAmount,
        decimal balance)
    {
        switch (paymentType)
        {
            case PaymentType.Regular:
                if (amount < installmentAmount &&
                    amount != balance)
                {
                    throw new InvalidOperationException(
                        "A regular payment cannot be less than the installment amount.");
                }

                break;

            case PaymentType.Partial:
                if (amount >= installmentAmount)
                {
                    throw new InvalidOperationException(
                        "A partial payment must be less than the installment amount.");
                }

                break;

            case PaymentType.FullSettlement:
                if (amount != balance)
                {
                    throw new InvalidOperationException(
                        "A full settlement payment must equal the outstanding balance.");
                }

                break;

            default:
                throw new InvalidOperationException(
                    "Invalid payment type.");
        }
    }

    private static void UpdateNextPaymentDate(
        Loan loan,
        decimal totalPaidBefore,
        decimal totalPaidAfter)
    {
        if (loan.InstallmentAmount <= 0)
        {
            return;
        }

        var completedBefore =
            (int)Math.Floor(
                totalPaidBefore / loan.InstallmentAmount);

        var completedAfter =
            (int)Math.Floor(
                totalPaidAfter / loan.InstallmentAmount);

        var newlyCompleted =
            completedAfter - completedBefore;

        if (newlyCompleted <= 0)
        {
            return;
        }

        for (var i = 0; i < newlyCompleted; i++)
        {
            loan.NextPaymentDate =
                AddFrequency(
                    loan.NextPaymentDate,
                    loan.PaymentFrequency);
        }
    }

    private static DateTime AddFrequency(
        DateTime date,
        PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily =>
                date.AddDays(1),

            PaymentFrequency.Weekly =>
                date.AddDays(7),

            PaymentFrequency.Biweekly =>
                date.AddDays(14),

            PaymentFrequency.Monthly =>
                date.AddMonths(1),

            _ => throw new InvalidOperationException(
                "Invalid payment frequency.")
        };
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static PaymentResponse Map(
        Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            TenantId = payment.TenantId,
            LoanId = payment.LoanId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentType = payment.PaymentType,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}