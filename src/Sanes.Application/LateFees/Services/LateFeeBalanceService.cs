using Sanes.Application.LateFees.DTOs;
using Sanes.Application.LateFees.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.Services;

public class LateFeeBalanceService : ILateFeeBalanceService
{
    private readonly ILateFeeRepository _lateFeeRepository;

    private readonly IPaymentAllocationRepository
        _paymentAllocationRepository;

    public LateFeeBalanceService(
        ILateFeeRepository lateFeeRepository,
        IPaymentAllocationRepository paymentAllocationRepository)
    {
        _lateFeeRepository = lateFeeRepository;
        _paymentAllocationRepository =
            paymentAllocationRepository;
    }

    public async Task<List<OutstandingLateFeeItem>>
        GetOutstandingByLoanAsync(
            Guid tenantId,
            Guid loanId,
            DateTime? effectiveDateCutoff = null,
            CancellationToken cancellationToken = default)
    {
        var charges =
            await _lateFeeRepository.GetByLoanAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (effectiveDateCutoff.HasValue)
        {
            charges = charges
                .Where(x =>
                    x.EffectiveDate <=
                    effectiveDateCutoff.Value)
                .ToList();
        }

        if (charges.Count == 0)
        {
            return new List<OutstandingLateFeeItem>();
        }

        var chargeIds = charges
            .Select(x => x.Id)
            .ToList();

        var paidByCharge =
            await _paymentAllocationRepository
                .GetTotalAppliedToLateFeesAsync(
                    tenantId,
                    chargeIds,
                    cancellationToken);

        return charges
            .Select(charge =>
                BuildItem(
                    charge,
                    paidByCharge.TryGetValue(
                        charge.Id,
                        out var paid)
                        ? paid
                        : 0m))
            .Where(x => x.OutstandingAmount > 0)
            .OrderBy(x => x.EffectiveDate)
            .ThenBy(x => x.InstallmentNumber)
            .ToList();
    }

    public async Task<decimal> GetOutstandingBalanceAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var outstanding =
            await GetOutstandingByLoanAsync(
                tenantId,
                loanId,
                null,
                cancellationToken);

        return outstanding.Sum(
            x => x.OutstandingAmount);
    }

    public async Task<Dictionary<Guid, decimal>>
        GetOutstandingBalancesByLoansAsync(
            Guid tenantId,
            IEnumerable<Guid> loanIds,
            CancellationToken cancellationToken = default)
    {
        var ids = loanIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var charges =
            await _lateFeeRepository.GetByLoansAsync(
                tenantId,
                ids,
                cancellationToken);

        var chargeIds = charges
            .Select(x => x.Id)
            .ToList();

        var paidByCharge =
            chargeIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await _paymentAllocationRepository
                    .GetTotalAppliedToLateFeesAsync(
                        tenantId,
                        chargeIds,
                        cancellationToken);

        var result = ids.ToDictionary(
            id => id,
            _ => 0m);

        foreach (var charge in charges)
        {
            var paid =
                paidByCharge.TryGetValue(
                    charge.Id,
                    out var amountPaid)
                    ? amountPaid
                    : 0m;

            var item =
                BuildItem(
                    charge,
                    paid);

            if (item.OutstandingAmount <= 0)
            {
                continue;
            }

            result[charge.LoanId] +=
                item.OutstandingAmount;
        }

        return result;
    }

    private static OutstandingLateFeeItem BuildItem(
        LateFeeCharge charge,
        decimal paidAmount)
    {
        var increaseAmount =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Increase)
                .Sum(x => x.Amount);

        var decreaseAmount =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Decrease)
                .Sum(x => x.Amount);

        var waivedAmount =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Waiver)
                .Sum(x => x.Amount);

        var adjustedAmount =
            charge.Amount +
            increaseAmount -
            decreaseAmount -
            waivedAmount;

        if (adjustedAmount < 0)
        {
            adjustedAmount = 0;
        }

        var outstandingAmount =
            adjustedAmount -
            paidAmount;

        if (outstandingAmount < 0)
        {
            outstandingAmount = 0;
        }

        return new OutstandingLateFeeItem
        {
            LateFeeChargeId = charge.Id,
            LoanId = charge.LoanId,

            InstallmentNumber =
                charge.InstallmentNumber,

            InstallmentDueDate =
                charge.InstallmentDueDate,

            EffectiveDate =
                charge.EffectiveDate,

            OriginalAmount =
                charge.Amount,

            IncreaseAmount =
                increaseAmount,

            DecreaseAmount =
                decreaseAmount,

            WaivedAmount =
                waivedAmount,

            PaidAmount =
                paidAmount,

            OutstandingAmount =
                outstandingAmount
        };
    }
}