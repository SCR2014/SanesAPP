using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Application.Clients.Repositories;
using Sanes.Application.LateFees.Services;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly ICollectionRouteRepository _collectionRouteRepository;
    private readonly IAppUserCollectionRouteRepository _appUserCollectionRouteRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPaymentAllocationRepository _paymentAllocationRepository;
    private readonly ILateFeeAccrualService _lateFeeAccrualService;
    private readonly ILateFeeBalanceService _lateFeeBalanceService;
    private readonly ILoanBalanceAdjustmentRepository _loanBalanceAdjustmentRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ILoanRepository loanRepository,
        ITenantRepository tenantRepository,
        IAppUserRepository appUserRepository,
        ICollectionRouteRepository collectionRouteRepository,
        IAppUserCollectionRouteRepository appUserCollectionRouteRepository,
        IClientRepository clientRepository,
        IPaymentAllocationRepository paymentAllocationRepository,
        ILateFeeAccrualService lateFeeAccrualService,
        ILateFeeBalanceService lateFeeBalanceService,
        ILoanBalanceAdjustmentRepository loanBalanceAdjustmentRepository)
    {
        _paymentRepository = paymentRepository;
        _loanRepository = loanRepository;
        _tenantRepository = tenantRepository;
        _appUserRepository = appUserRepository;
        _collectionRouteRepository = collectionRouteRepository;
        _appUserCollectionRouteRepository = appUserCollectionRouteRepository;
        _clientRepository = clientRepository;
        _paymentAllocationRepository = paymentAllocationRepository;
        _lateFeeAccrualService = lateFeeAccrualService;
        _lateFeeBalanceService = lateFeeBalanceService;
        _loanBalanceAdjustmentRepository = loanBalanceAdjustmentRepository;
    }

    public async Task<PaymentResponse> CreateAsync(
        Guid tenantId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            tenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            throw new InvalidOperationException(
                "Tenant not found or inactive.");
        }

        var loan = await _loanRepository.GetByIdForUpdateAsync(
            tenantId,
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

        if (request.CollectedByAppUserId.HasValue ||
            request.CollectionRouteId.HasValue)
        {
            await ValidateFieldCollectionContextAsync(
                tenantId,
                request,
                loan,
                cancellationToken);
        }

        var paymentDate =
            NormalizeUtc(request.PaymentDate);

        await _lateFeeAccrualService.AccrueForLoanAsync(
            tenantId,
            request.LoanId,
            paymentDate,
            cancellationToken);

        var totalAmount =
            loan.InstallmentAmount * loan.TotalInstallments;

        var totalAppliedToLoanBefore =
            await _paymentAllocationRepository
                .GetTotalAppliedToLoanAsync(
                    tenantId,
                    request.LoanId,
                    cancellationToken);

        var contractualAdjustmentsBefore =
            await _loanBalanceAdjustmentRepository
                .GetTotalReductionsByLoanAsync(
                    tenantId,
                    request.LoanId,
                    cancellationToken);

        var loanBalanceBefore =
            totalAmount - totalAppliedToLoanBefore - contractualAdjustmentsBefore;

        if (loanBalanceBefore < 0)
        {
            loanBalanceBefore = 0;
        }

        var outstandingLateFees =
            await _lateFeeBalanceService
                .GetOutstandingByLoanAsync(
                    tenantId,
                    request.LoanId,
                    paymentDate,
                    cancellationToken);

        var lateFeeBalanceBefore =
            outstandingLateFees.Sum(
                x => x.OutstandingAmount);

        var totalOutstandingBefore =
            loanBalanceBefore +
            lateFeeBalanceBefore;

        if (totalOutstandingBefore <= 0)
        {
            throw new InvalidOperationException(
                "This loan has no outstanding balance.");
        }

        if (request.Amount > totalOutstandingBefore)
        {
            throw new InvalidOperationException(
                $"Payment amount cannot exceed the total outstanding balance of {totalOutstandingBefore:0.00}.");
        }

        var amountAppliedToLateFees =
            Math.Min(
                request.Amount,
                lateFeeBalanceBefore);

        var amountAppliedToLoan =
            request.Amount -
            amountAppliedToLateFees;

        ValidatePaymentType(
            request.PaymentType,
            request.Amount,
            amountAppliedToLoan,
            loan.InstallmentAmount,
            loanBalanceBefore,
            totalOutstandingBefore);

        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            TenantId = tenantId,
            LoanId = request.LoanId,
            Amount = request.Amount,
            PaymentDate = paymentDate,
            PaymentType = request.PaymentType,
            CollectedByAppUserId = request.CollectedByAppUserId,
            CollectionRouteId = request.CollectionRouteId,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken);

        var remainingForLateFees =
            amountAppliedToLateFees;

        foreach (var lateFee in outstandingLateFees)
        {
            if (remainingForLateFees <= 0)
            {
                break;
            }

            var allocationAmount =
                Math.Min(
                    remainingForLateFees,
                    lateFee.OutstandingAmount);

            var allocation =
                new PaymentAllocation
                {
                    TenantId = tenantId,
                    PaymentId = payment.Id,
                    AllocationType =
                        PaymentAllocationType.LateFee,
                    LateFeeChargeId =
                        lateFee.LateFeeChargeId,
                    Amount = allocationAmount,
                    CreatedAt = now
                };

            await _paymentAllocationRepository.AddAsync(
                allocation,
                cancellationToken);

            remainingForLateFees -=
                allocationAmount;
        }

        if (amountAppliedToLoan > 0)
        {
            var loanAllocation =
                new PaymentAllocation
                {
                    TenantId = tenantId,
                    PaymentId = payment.Id,
                    AllocationType =
                        PaymentAllocationType.LoanBalance,
                    LateFeeChargeId = null,
                    Amount = amountAppliedToLoan,
                    CreatedAt = now
                };

            await _paymentAllocationRepository.AddAsync(
                loanAllocation,
                cancellationToken);
        }

        var totalAppliedToLoanAfter =
            totalAppliedToLoanBefore +
            amountAppliedToLoan;

        var loanBalanceAfter =
            totalAmount -
            totalAppliedToLoanAfter;

        if (loanBalanceAfter < 0)
        {
            loanBalanceAfter = 0;
        }

        var lateFeeBalanceAfter =
            lateFeeBalanceBefore -
            amountAppliedToLateFees;

        if (lateFeeBalanceAfter < 0)
        {
            lateFeeBalanceAfter = 0;
        }

        if (
            loanBalanceAfter == 0 &&
            lateFeeBalanceAfter == 0)
        {
            loan.Status = LoanStatus.Paid;
        }
        else if (amountAppliedToLoan > 0)
        {
            UpdateNextPaymentDate(
                loan,
                totalAppliedToLoanBefore,
                totalAppliedToLoanAfter);
        }

        loan.UpdatedAt = now;

        /*
        * PaymentRepository, PaymentAllocationRepository
        * y LoanRepository utilizan el mismo SanesDbContext
        * dentro del mismo scope.
        *
        * Un único SaveChangesAsync persiste Payment,
        * allocations y cambios del Loan.
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
        decimal amountReceived,
        decimal amountAppliedToLoan,
        decimal installmentAmount,
        decimal loanBalance,
        decimal totalOutstanding)
    {
        switch (paymentType)
        {
            case PaymentType.Regular:
                if (
                    loanBalance > 0 &&
                    amountAppliedToLoan < installmentAmount &&
                    amountAppliedToLoan != loanBalance)
                {
                    throw new InvalidOperationException(
                        "A regular payment must apply at least one installment amount to the loan balance.");
                }

                break;

            case PaymentType.Partial:
                if (
                    amountReceived == totalOutstanding)
                {
                    throw new InvalidOperationException(
                        "A partial payment cannot settle the entire outstanding balance.");
                }

                if (
                    loanBalance > 0 &&
                    amountAppliedToLoan >= installmentAmount)
                {
                    throw new InvalidOperationException(
                        "A partial payment must apply less than one installment amount to the loan balance.");
                }

                break;

            case PaymentType.FullSettlement:
                if (amountReceived != totalOutstanding)
                {
                    throw new InvalidOperationException(
                        "A full settlement payment must equal the total outstanding balance.");
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
            CollectedByAppUserId =
                payment.CollectedByAppUserId,
            CollectionRouteId =
                payment.CollectionRouteId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentType = payment.PaymentType,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    private async Task ValidateFieldCollectionContextAsync(
        Guid tenantId,
        CreatePaymentRequest request,
        Loan loan,
        CancellationToken cancellationToken)
    {
        if (!request.CollectedByAppUserId.HasValue ||
            !request.CollectionRouteId.HasValue)
        {
            throw new InvalidOperationException(
                "CollectedByAppUserId and CollectionRouteId must be provided together.");
        }

        var collector =
            await _appUserRepository.GetByIdAsync(
                request.CollectedByAppUserId.Value,
                tenantId,
                cancellationToken);

        if (collector is null)
        {
            throw new InvalidOperationException(
                "Collector not found, inactive, or does not belong to the specified tenant.");
        }

        if (collector.Role != AppUserRole.Collector)
        {
            throw new InvalidOperationException(
                "The selected app user is not a collector.");
        }

        var route =
            await _collectionRouteRepository.GetByIdAsync(
                request.CollectionRouteId.Value,
                tenantId,
                cancellationToken);

        if (route is null)
        {
            throw new InvalidOperationException(
                "Collection route not found, inactive, or does not belong to the specified tenant.");
        }

        var collectorAssignedToRoute =
            await _appUserCollectionRouteRepository.ExistsAsync(
                collector.Id,
                route.Id,
                cancellationToken);

        if (!collectorAssignedToRoute)
        {
            throw new InvalidOperationException(
                "The collector is not assigned to the specified collection route.");
        }

        var client =
            await _clientRepository.GetByIdAsync(
                tenantId,
                loan.ClientId,
                cancellationToken);

        if (client is null)
        {
            throw new InvalidOperationException(
                "Loan client not found, inactive, or does not belong to the specified tenant.");
        }

        if (client.CollectionRouteId != route.Id)
        {
            throw new InvalidOperationException(
                "The loan client does not belong to the specified collection route.");
        }
    }

}