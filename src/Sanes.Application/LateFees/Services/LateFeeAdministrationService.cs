using Sanes.Application.LateFees.DTOs;
using Sanes.Application.LateFees.Repositories;
using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.LateFees.Services;

public class LateFeeAdministrationService
    : ILateFeeAdministrationService
{
    private readonly ILateFeeRepository _lateFeeRepository;
    private readonly ILateFeeAccrualService _lateFeeAccrualService;
    private readonly ILateFeeBalanceService _lateFeeBalanceService;
    private readonly IPaymentAllocationRepository
        _paymentAllocationRepository;
    private readonly ILoanRepository _loanRepository;

    public LateFeeAdministrationService(
        ILateFeeRepository lateFeeRepository,
        ILateFeeAccrualService lateFeeAccrualService,
        ILateFeeBalanceService lateFeeBalanceService,
        IPaymentAllocationRepository paymentAllocationRepository,
        ILoanRepository loanRepository)
    {
        _lateFeeRepository = lateFeeRepository;
        _lateFeeAccrualService = lateFeeAccrualService;
        _lateFeeBalanceService = lateFeeBalanceService;
        _paymentAllocationRepository =
            paymentAllocationRepository;
        _loanRepository = loanRepository;
    }

    public async Task<LateFeeLoanResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan =
            await _loanRepository.GetByIdAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null)
        {
            return null;
        }

        /*
         * Antes de mostrar el historial aseguramos que todas
         * las moras que debieron existir hasta hoy hayan sido
         * materializadas.
         */
        await _lateFeeAccrualService.AccrueForLoanAsync(
            tenantId,
            loanId,
            DateTime.UtcNow,
            cancellationToken);

        var charges =
            await _lateFeeRepository.GetByLoanAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (charges.Count == 0)
        {
            return new LateFeeLoanResponse
            {
                LoanId = loanId
            };
        }

        var chargeIds =
            charges
                .Select(x => x.Id)
                .ToList();

        var paidByCharge =
            await _paymentAllocationRepository
                .GetTotalAppliedToLateFeesAsync(
                    tenantId,
                    chargeIds,
                    cancellationToken);

        var chargeResponses =
            charges
                .Select(charge =>
                    MapCharge(
                        charge,
                        paidByCharge.TryGetValue(
                            charge.Id,
                            out var paid)
                            ? paid
                            : 0m))
                .ToList();

        return new LateFeeLoanResponse
        {
            LoanId =
                loanId,

            TotalOriginalCharges =
                chargeResponses.Sum(
                    x => x.OriginalAmount),

            TotalAdjustedCharges =
                chargeResponses.Sum(
                    x => x.AdjustedAmount),

            TotalPaidToLateFees =
                chargeResponses.Sum(
                    x => x.PaidAmount),

            LateFeeBalance =
                chargeResponses.Sum(
                    x => x.OutstandingAmount),

            Charges =
                chargeResponses
        };
    }

    public async Task<LateFeeChargeResponse?>
        AddAdjustmentAsync(
            Guid tenantId,
            Guid appUserId,
            Guid lateFeeChargeId,
            LateFeeAdjustmentRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var charge =
            await _lateFeeRepository.GetChargeForUpdateAsync(
                tenantId,
                lateFeeChargeId,
                cancellationToken);

        if (charge is null)
        {
            return null;
        }

        var loan =
            await _loanRepository.GetByIdForUpdateAsync(
                tenantId,
                charge.LoanId,
                cancellationToken);

        if (loan is null)
        {
            throw new InvalidOperationException(
                "Loan associated with the late fee charge was not found.");
        }

        var paidAmount =
            await _paymentAllocationRepository
                .GetTotalAppliedToLateFeeAsync(
                    tenantId,
                    charge.Id,
                    cancellationToken);

        var currentAdjustedAmount =
            CalculateAdjustedAmount(charge);

        var currentOutstanding =
            currentAdjustedAmount -
            paidAmount;

        if (currentOutstanding < 0)
        {
            currentOutstanding = 0;
        }

        /*
         * Increase siempre puede aumentar el cargo.
         *
         * Decrease y Waiver solamente pueden actuar sobre
         * el saldo que todavía está pendiente.
         *
         * No permitimos reducir por debajo de dinero que
         * ya fue pagado.
         */
        if (
            request.AdjustmentType ==
                LateFeeAdjustmentType.Decrease ||
            request.AdjustmentType ==
                LateFeeAdjustmentType.Waiver)
        {
            if (currentOutstanding <= 0)
            {
                throw new InvalidOperationException(
                    "This late fee charge has no outstanding balance to reduce or waive.");
            }

            if (request.Amount > currentOutstanding)
            {
                throw new InvalidOperationException(
                    $"Adjustment amount cannot exceed the outstanding late fee balance of {currentOutstanding:0.00}.");
            }
        }

        /*
         * Obtenemos el saldo total del préstamo antes del
         * nuevo ajuste. Esto permite mantener correctamente
         * el estado Active/Paid.
         */
        var lateFeeBalanceBefore =
            await _lateFeeBalanceService
                .GetOutstandingBalanceAsync(
                    tenantId,
                    charge.LoanId,
                    cancellationToken);

        var reason =
            request.Reason.Trim();

        var adjustment =
            new LateFeeAdjustment
            {
                TenantId =
                    tenantId,

                LateFeeChargeId =
                    charge.Id,

                AppUserId =
                    appUserId,

                AdjustmentType =
                    request.AdjustmentType,

                Amount =
                    request.Amount,

                Reason =
                    reason,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _lateFeeRepository.AddAdjustmentAsync(
            adjustment,
            cancellationToken);

        var lateFeeDelta =
            request.AdjustmentType ==
                LateFeeAdjustmentType.Increase
                ? request.Amount
                : -request.Amount;

        var lateFeeBalanceAfter =
            lateFeeBalanceBefore +
            lateFeeDelta;

        if (lateFeeBalanceAfter < 0)
        {
            lateFeeBalanceAfter = 0;
        }

        /*
         * Revisamos también el saldo contractual para mantener
         * coherente Loan.Status.
         */
        var totalAmount =
            loan.InstallmentAmount *
            loan.TotalInstallments;

        var totalAppliedToLoan =
            await _paymentAllocationRepository
                .GetTotalAppliedToLoanAsync(
                    tenantId,
                    loan.Id,
                    cancellationToken);

        var contractualBalance =
            totalAmount -
            totalAppliedToLoan;

        if (contractualBalance < 0)
        {
            contractualBalance = 0;
        }

        /*
         * Un préstamo cancelado conserva su estado histórico.
         *
         * Para Active/Paid:
         * - si ya no existe ninguna deuda => Paid
         * - si existe deuda contractual o de mora => Active
         */
        if (loan.Status != LoanStatus.Cancelled)
        {
            if (
                contractualBalance == 0 &&
                lateFeeBalanceAfter == 0)
            {
                loan.Status =
                    LoanStatus.Paid;
            }
            else
            {
                loan.Status =
                    LoanStatus.Active;
            }

            loan.UpdatedAt =
                DateTime.UtcNow;
        }

        /*
         * LateFeeRepository y LoanRepository comparten
         * SanesDbContext dentro del mismo scope, por lo que
         * este SaveChanges persiste ajuste + estado del Loan.
         */
        await _lateFeeRepository.SaveChangesAsync(
            cancellationToken);

        var updatedCharge =
            await _lateFeeRepository.GetChargeByIdAsync(
                tenantId,
                charge.Id,
                cancellationToken);

        if (updatedCharge is null)
        {
            throw new InvalidOperationException(
                "Unable to retrieve the late fee charge after adjustment.");
        }

        var updatedPaidAmount =
            await _paymentAllocationRepository
                .GetTotalAppliedToLateFeeAsync(
                    tenantId,
                    updatedCharge.Id,
                    cancellationToken);

        return MapCharge(
            updatedCharge,
            updatedPaidAmount);
    }

    private static void ValidateRequest(
        LateFeeAdjustmentRequest request)
    {
        if (!Enum.IsDefined(
                typeof(LateFeeAdjustmentType),
                request.AdjustmentType))
        {
            throw new InvalidOperationException(
                "Late fee adjustment type is invalid.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Adjustment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            throw new InvalidOperationException(
                "Adjustment reason is required.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new InvalidOperationException(
                "Adjustment reason cannot exceed 500 characters.");
        }
    }

    private static decimal CalculateAdjustedAmount(
        LateFeeCharge charge)
    {
        var increases =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Increase)
                .Sum(x => x.Amount);

        var decreases =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Decrease)
                .Sum(x => x.Amount);

        var waivers =
            charge.Adjustments
                .Where(x =>
                    x.AdjustmentType ==
                    LateFeeAdjustmentType.Waiver)
                .Sum(x => x.Amount);

        var adjustedAmount =
            charge.Amount +
            increases -
            decreases -
            waivers;

        return adjustedAmount < 0
            ? 0
            : adjustedAmount;
    }

    private static LateFeeChargeResponse MapCharge(
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

        return new LateFeeChargeResponse
        {
            Id =
                charge.Id,

            LoanId =
                charge.LoanId,

            InstallmentNumber =
                charge.InstallmentNumber,

            InstallmentDueDate =
                charge.InstallmentDueDate,

            EffectiveDate =
                charge.EffectiveDate,

            CalculationType =
                charge.CalculationType,

            OriginalAmount =
                charge.Amount,

            IncreaseAmount =
                increaseAmount,

            DecreaseAmount =
                decreaseAmount,

            WaivedAmount =
                waivedAmount,

            AdjustedAmount =
                adjustedAmount,

            PaidAmount =
                paidAmount,

            OutstandingAmount =
                outstandingAmount,

            CreatedAt =
                charge.CreatedAt,

            Adjustments =
                charge.Adjustments
                    .OrderBy(x => x.CreatedAt)
                    .Select(x =>
                        new LateFeeAdjustmentResponse
                        {
                            Id =
                                x.Id,

                            LateFeeChargeId =
                                x.LateFeeChargeId,

                            AppUserId =
                                x.AppUserId,

                            AdjustmentType =
                                x.AdjustmentType,

                            Amount =
                                x.Amount,

                            Reason =
                                x.Reason,

                            CreatedAt =
                                x.CreatedAt
                        })
                    .ToList()
        };
    }
}