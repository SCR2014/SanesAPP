using Sanes.Application.LateFees.Repositories;
using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.Services;

public class LateFeeAccrualService : ILateFeeAccrualService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ILateFeeRepository _lateFeeRepository;
    private readonly IPaymentAllocationRepository
        _paymentAllocationRepository;

    public LateFeeAccrualService(
        ILoanRepository loanRepository,
        ILateFeeRepository lateFeeRepository,
        IPaymentAllocationRepository paymentAllocationRepository)
    {
        _loanRepository = loanRepository;
        _lateFeeRepository = lateFeeRepository;
        _paymentAllocationRepository =
            paymentAllocationRepository;
    }

    public async Task<int> AccrueForLoanAsync(
        Guid tenantId,
        Guid loanId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(
            tenantId,
            loanId,
            cancellationToken);

        if (loan is null)
        {
            return 0;
        }

        return await AccrueAsync(
            tenantId,
            new[] { loan },
            asOfDate,
            cancellationToken);
    }

    public async Task<int> AccrueForTenantAsync(
        Guid tenantId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var loans =
            await _loanRepository.GetActiveByTenantAsync(
                tenantId,
                cancellationToken);

        return await AccrueAsync(
            tenantId,
            loans,
            asOfDate,
            cancellationToken);
    }

    private async Task<int> AccrueAsync(
        Guid tenantId,
        IEnumerable<Loan> loans,
        DateTime? asOfDate,
        CancellationToken cancellationToken)
    {
        var eligibleLoans = loans
            .Where(x =>
                x.Status == LoanStatus.Active &&
                x.LateFeeEnabled)
            .ToList();

        if (eligibleLoans.Count == 0)
        {
            return 0;
        }

        foreach (var loan in eligibleLoans)
        {
            if (
                loan.LateFeeCalculationType !=
                LateFeeCalculationType.FixedAmountPerInstallment)
            {
                throw new InvalidOperationException(
                    "Only fixed late fees are currently supported.");
            }

            if (loan.LateFeeAmount <= 0)
            {
                throw new InvalidOperationException(
                    "Late fee amount must be greater than zero.");
            }

            if (loan.LateFeeGraceDays < 0)
            {
                throw new InvalidOperationException(
                    "Late fee grace days cannot be negative.");
            }
        }

        var normalizedAsOfDate =
            NormalizeAsOfDate(asOfDate);

        var loanIds = eligibleLoans
            .Select(x => x.Id)
            .ToList();

        var allocations =
            await _paymentAllocationRepository
                .GetLoanBalanceAllocationsByLoansAsync(
                    tenantId,
                    loanIds,
                    cancellationToken);

        var existingCharges =
            await _lateFeeRepository.GetByLoansAsync(
                tenantId,
                loanIds,
                cancellationToken);

        var allocationsByLoan =
            allocations
                .GroupBy(x => x.Payment.LoanId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

        var existingKeys =
            existingCharges
                .Select(x => new LateFeeKey(
                    x.LoanId,
                    x.InstallmentNumber,
                    x.EffectiveDate))
                .ToHashSet();

        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var loan in eligibleLoans)
        {
            allocationsByLoan.TryGetValue(
                loan.Id,
                out var loanAllocations);

            loanAllocations ??=
                new List<PaymentAllocation>();

            var dueDate =
                AddFrequency(
                    loan.StartDate,
                    loan.PaymentFrequency);

            for (
                var installmentNumber = 1;
                installmentNumber <= loan.TotalInstallments;
                installmentNumber++)
            {
                var installmentDueDate =
                    NormalizeScheduleDate(dueDate);

                var effectiveDate =
                    installmentDueDate
                        .AddDays(
                            1 +
                            loan.LateFeeGraceDays);

                /*
                 * Si todavía no llegó la fecha efectiva,
                 * tampoco podrán haber llegado las siguientes
                 * cuotas, por lo que dejamos de recorrer.
                 */
                if (effectiveDate > normalizedAsOfDate)
                {
                    break;
                }

                var key =
                    new LateFeeKey(
                        loan.Id,
                        installmentNumber,
                        effectiveDate);

                if (existingKeys.Contains(key))
                {
                    dueDate =
                        AddFrequency(
                            dueDate,
                            loan.PaymentFrequency);

                    continue;
                }

                /*
                 * Una cuota evita la mora únicamente si el dinero
                 * necesario para cubrirla ya estaba aplicado al
                 * préstamo ANTES de comenzar su fecha efectiva.
                 *
                 * Un pago realizado el mismo día en que comienza
                 * la mora ya se considera tardío.
                 */
                var totalAppliedBeforeEffectiveDate =
                    loanAllocations
                        .Where(x =>
                            x.Payment.PaymentDate <
                            effectiveDate)
                        .Sum(x => x.Amount);

                var cumulativeAmountRequired =
                    loan.InstallmentAmount *
                    installmentNumber;

                if (
                    totalAppliedBeforeEffectiveDate <
                    cumulativeAmountRequired)
                {
                    var charge =
                        new LateFeeCharge
                        {
                            TenantId = tenantId,
                            LoanId = loan.Id,

                            InstallmentNumber =
                                installmentNumber,

                            InstallmentDueDate =
                                installmentDueDate,

                            EffectiveDate =
                                effectiveDate,

                            CalculationType =
                                loan.LateFeeCalculationType,

                            Amount =
                                loan.LateFeeAmount,

                            CreatedAt = now
                        };

                    await _lateFeeRepository.AddChargeAsync(
                        charge,
                        cancellationToken);

                    existingKeys.Add(key);
                    created++;
                }

                dueDate =
                    AddFrequency(
                        dueDate,
                        loan.PaymentFrequency);
            }
        }

        if (created > 0)
        {
            await _lateFeeRepository.SaveChangesAsync(
                cancellationToken);
        }

        return created;
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

    private static DateTime NormalizeScheduleDate(
        DateTime value)
    {
        return DateTime.SpecifyKind(
            value.Date,
            DateTimeKind.Utc);
    }

    private static DateTime NormalizeAsOfDate(
        DateTime? value)
    {
        var source =
            value ?? DateTime.UtcNow;

        DateTime utcValue;

        if (source.Kind == DateTimeKind.Utc)
        {
            utcValue = source;
        }
        else if (source.Kind == DateTimeKind.Local)
        {
            utcValue = source.ToUniversalTime();
        }
        else
        {
            utcValue = DateTime.SpecifyKind(
                source,
                DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(
            utcValue.Date,
            DateTimeKind.Utc);
    }

    private sealed record LateFeeKey(
        Guid LoanId,
        int InstallmentNumber,
        DateTime EffectiveDate);
}