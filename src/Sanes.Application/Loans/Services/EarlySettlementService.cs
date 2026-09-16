using Sanes.Application.LateFees.Services;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Application.Common.Persistence;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.Services;

public class EarlySettlementService
    : IEarlySettlementService
{
    private const int MinimumCompletedInstallments = 6;

    private readonly ILoanRepository _loanRepository;

    private readonly IPaymentAllocationRepository
        _paymentAllocationRepository;

    private readonly ILoanBalanceAdjustmentRepository
        _loanBalanceAdjustmentRepository;

    private readonly IEarlySettlementRepository
        _earlySettlementRepository;

    private readonly ILateFeeAccrualService
        _lateFeeAccrualService;

    private readonly ILateFeeBalanceService
        _lateFeeBalanceService;

    private readonly IPaymentService
        _paymentService;

    private readonly ITransactionRunner
        _transactionRunner;

    public EarlySettlementService(
        ILoanRepository loanRepository,
        IPaymentAllocationRepository paymentAllocationRepository,
        ILoanBalanceAdjustmentRepository
            loanBalanceAdjustmentRepository,
        IEarlySettlementRepository earlySettlementRepository,
        ILateFeeAccrualService lateFeeAccrualService,
        ILateFeeBalanceService lateFeeBalanceService,
        IPaymentService paymentService,
        ITransactionRunner transactionRunner)
    {
        _loanRepository = loanRepository;

        _paymentAllocationRepository =
            paymentAllocationRepository;

        _loanBalanceAdjustmentRepository =
            loanBalanceAdjustmentRepository;

        _earlySettlementRepository =
            earlySettlementRepository;

        _lateFeeAccrualService =
            lateFeeAccrualService;

        _lateFeeBalanceService =
            lateFeeBalanceService;

        _paymentService =
            paymentService;

        _transactionRunner =
            transactionRunner;
    }

    public async Task<EarlySettlementQuoteResponse?> QuoteAsync(
        Guid tenantId,
        Guid loanId,
        EarlySettlementQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var loan =
            await _loanRepository.GetByIdAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null)
        {
            return null;
        }

        if (loan.Status == LoanStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cancelled loans cannot be settled early.");
        }

        if (loan.Status == LoanStatus.Paid)
        {
            throw new InvalidOperationException(
                "The loan is already paid.");
        }

        var alreadySettled =
            await _earlySettlementRepository
                .ExistsForLoanAsync(
                    tenantId,
                    loanId,
                    cancellationToken);

        if (alreadySettled)
        {
            throw new InvalidOperationException(
                "The loan has already been settled early.");
        }

        /*
         * Antes de cotizar debemos materializar cualquier
         * mora que corresponda hasta este momento.
         */
        var quotedAt =
            DateTime.UtcNow;

        await _lateFeeAccrualService.AccrueForLoanAsync(
            tenantId,
            loanId,
            quotedAt,
            cancellationToken);

        var totalAmount =
            loan.InstallmentAmount *
            loan.TotalInstallments;

        var amountPaid =
            await _paymentAllocationRepository
                .GetTotalAppliedToLoanAsync(
                    tenantId,
                    loanId,
                    cancellationToken);

        var contractualAdjustments =
            await _loanBalanceAdjustmentRepository
                .GetTotalReductionsByLoanAsync(
                    tenantId,
                    loanId,
                    cancellationToken);

        var contractualBalance =
            totalAmount
            - amountPaid
            - contractualAdjustments;

        if (contractualBalance < 0)
        {
            contractualBalance = 0;
        }
        if (contractualBalance <= 0)
        {
            throw new InvalidOperationException(
                "The loan has no contractual balance eligible for early settlement.");
        }

        /*
         * Para elegibilidad contamos cuotas equivalentes
         * realmente pagadas en efectivo al contrato.
         *
         * Los descuentos contractuales no cuentan como
         * cuotas pagadas.
         */
        var completedInstallments =
            loan.InstallmentAmount > 0
                ? (int)Math.Floor(
                    amountPaid /
                    loan.InstallmentAmount)
                : 0;

        if (
            completedInstallments <
            MinimumCompletedInstallments)
        {
            throw new InvalidOperationException(
                $"At least {MinimumCompletedInstallments} completed installments are required for early settlement. Current completed installments: {completedInstallments}.");
        }

        var lateFeeBalance =
            await _lateFeeBalanceService
                .GetOutstandingBalanceAsync(
                    tenantId,
                    loanId,
                    cancellationToken);

        var totalOutstanding =
            contractualBalance +
            lateFeeBalance;

        var discountAmount =
            CalculateDiscount(
                loan.InstallmentAmount,
                contractualBalance,
                request);

        /*
         * El descuento solo afecta el saldo contractual.
         * Nunca reducimos automáticamente la mora.
         */
        if (discountAmount > contractualBalance)
        {
            discountAmount =
                contractualBalance;
        }

        if (discountAmount <= 0)
        {
            throw new InvalidOperationException(
                "The selected discount does not produce a monetary discount.");
        }

        var settlementAmount =
            contractualBalance
            - discountAmount
            + lateFeeBalance;

        if (settlementAmount < 0)
        {
            settlementAmount = 0;
        }

        return new EarlySettlementQuoteResponse
        {
            LoanId =
                loan.Id,

            IsEligible =
                true,

            MinimumRequiredInstallments =
                MinimumCompletedInstallments,

            CompletedInstallments =
                completedInstallments,

            InstallmentAmount =
                loan.InstallmentAmount,

            AmountPaid =
                amountPaid,

            ContractualAdjustments =
                contractualAdjustments,

            ContractualBalance =
                contractualBalance,

            LateFeeBalance =
                lateFeeBalance,

            TotalOutstanding =
                totalOutstanding,

            DiscountType =
                request.DiscountType,

            DiscountValue =
                request.DiscountValue,

            DiscountAmount =
                discountAmount,

            SettlementAmount =
                settlementAmount,

            QuotedAt =
                quotedAt
        };
    }

    public async Task<EarlySettlementResponse?> ExecuteAsync(
        Guid tenantId,
        Guid appUserId,
        Guid loanId,
        EarlySettlementExecuteRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateExecuteRequest(request);

        return await _transactionRunner
            .ExecuteAsync<EarlySettlementResponse?>(
                async transactionCancellationToken =>
                {
                    /*
                    * Recalculamos completamente la cotización.
                    * Nunca confiamos en saldos o descuentos enviados
                    * previamente por el cliente.
                    */
                    var quote =
                        await QuoteAsync(
                            tenantId,
                            loanId,
                            new EarlySettlementQuoteRequest
                            {
                                DiscountType =
                                    request.DiscountType,

                                DiscountValue =
                                    request.DiscountValue
                            },
                            transactionCancellationToken);

                    if (quote is null)
                    {
                        return null;
                    }

                    var expectedSettlementAmount =
                        Math.Round(
                            request.ExpectedSettlementAmount,
                            2,
                            MidpointRounding.AwayFromZero);

                    if (
                        expectedSettlementAmount !=
                        quote.SettlementAmount)
                    {
                        throw new InvalidOperationException(
                            $"The settlement amount has changed. Expected {expectedSettlementAmount:0.00}, current amount is {quote.SettlementAmount:0.00}. Request a new quote before continuing.");
                    }

                    var reason =
                        request.Reason.Trim();

                    /*
                    * Primero registramos el beneficio contractual.
                    *
                    * PaymentService debe verlo antes de crear el pago
                    * para que el nuevo saldo contractual ya incluya
                    * el descuento.
                    */
                    var adjustment =
                        new LoanBalanceAdjustment
                        {
                            TenantId =
                                tenantId,

                            LoanId =
                                loanId,

                            AppUserId =
                                appUserId,

                            AdjustmentType =
                                LoanBalanceAdjustmentType
                                    .EarlySettlementDiscount,

                            Amount =
                                quote.DiscountAmount,

                            Reason =
                                reason,

                            CreatedAt =
                                quote.QuotedAt
                        };

                    await _loanBalanceAdjustmentRepository
                        .AddAsync(
                            adjustment,
                            transactionCancellationToken);

                    /*
                    * Este SaveChanges no hace COMMIT.
                    *
                    * Sigue dentro de ITransactionRunner, pero permite
                    * que PaymentService vea el ajuste cuando vuelva
                    * a calcular el saldo contractual.
                    */
                    await _loanBalanceAdjustmentRepository
                        .SaveChangesAsync(
                            transactionCancellationToken);

                    Guid? paymentId =
                        null;

                    /*
                    * Solamente existe Payment si realmente entró
                    * dinero.
                    */
                    if (quote.SettlementAmount > 0)
                    {
                        var payment =
                            await _paymentService.CreateAsync(
                                tenantId,
                                new CreatePaymentRequest
                                {
                                    LoanId =
                                        loanId,

                                    Amount =
                                        quote.SettlementAmount,

                                    PaymentDate =
                                        quote.QuotedAt,

                                    PaymentType =
                                        PaymentType.FullSettlement,

                                    Notes =
                                        "Early settlement"
                                },
                                transactionCancellationToken);

                        paymentId =
                            payment.Id;
                    }

                    /*
                    * Verificamos financieramente que la operación
                    * realmente haya extinguido toda la deuda.
                    */
                    var loan =
                        await _loanRepository.GetByIdForUpdateAsync(
                            tenantId,
                            loanId,
                            transactionCancellationToken);

                    if (loan is null)
                    {
                        throw new InvalidOperationException(
                            "Loan could not be retrieved after early settlement.");
                    }

                    var totalAmount =
                        loan.InstallmentAmount *
                        loan.TotalInstallments;

                    var amountPaidAfter =
                        await _paymentAllocationRepository
                            .GetTotalAppliedToLoanAsync(
                                tenantId,
                                loanId,
                                transactionCancellationToken);

                    var contractualAdjustmentsAfter =
                        await _loanBalanceAdjustmentRepository
                            .GetTotalReductionsByLoanAsync(
                                tenantId,
                                loanId,
                                transactionCancellationToken);

                    var contractualBalanceAfter =
                        totalAmount
                        - amountPaidAfter
                        - contractualAdjustmentsAfter;

                    if (contractualBalanceAfter < 0)
                    {
                        contractualBalanceAfter = 0;
                    }

                    var lateFeeBalanceAfter =
                        await _lateFeeBalanceService
                            .GetOutstandingBalanceAsync(
                                tenantId,
                                loanId,
                                transactionCancellationToken);

                    if (
                        contractualBalanceAfter != 0 ||
                        lateFeeBalanceAfter != 0)
                    {
                        throw new InvalidOperationException(
                            "Early settlement did not fully clear the outstanding loan balance.");
                    }

                    /*
                    * Una liquidación completada correctamente debe
                    * dejar el Loan como Paid.
                    *
                    * Esto también cubre el caso SettlementAmount = 0,
                    * donde no existe Payment que cambie el estado.
                    */
                    if (loan.Status != LoanStatus.Cancelled)
                    {
                        loan.Status =
                            LoanStatus.Paid;

                        loan.UpdatedAt =
                            quote.QuotedAt;
                    }

                    var earlySettlement =
                        new EarlySettlement
                        {
                            TenantId =
                                tenantId,

                            LoanId =
                                loanId,

                            AppUserId =
                                appUserId,

                            PaymentId =
                                paymentId,

                            LoanBalanceAdjustmentId =
                                adjustment.Id,

                            DiscountType =
                                quote.DiscountType,

                            DiscountValue =
                                quote.DiscountValue,

                            CompletedInstallments =
                                quote.CompletedInstallments,

                            ContractualBalanceBefore =
                                quote.ContractualBalance,

                            LateFeeBalanceBefore =
                                quote.LateFeeBalance,

                            TotalOutstandingBefore =
                                quote.TotalOutstanding,

                            DiscountAmount =
                                quote.DiscountAmount,

                            SettlementAmount =
                                quote.SettlementAmount,

                            CreatedAt =
                                quote.QuotedAt
                        };

                    await _earlySettlementRepository
                        .AddAsync(
                            earlySettlement,
                            transactionCancellationToken);

                    /*
                    * Todos los repositorios comparten el mismo
                    * SanesDbContext. Este SaveChanges persiste:
                    *
                    * - Loan.Status
                    * - EarlySettlement
                    *
                    * Payment y Adjustment ya hicieron SaveChanges,
                    * pero continúan dentro de la misma transacción.
                    */
                    await _earlySettlementRepository
                        .SaveChangesAsync(
                            transactionCancellationToken);

                    var appliedToLateFees =
                        quote.LateFeeBalance;

                    var appliedToLoan =
                        quote.ContractualBalance
                        - quote.DiscountAmount;

                    if (appliedToLoan < 0)
                    {
                        appliedToLoan = 0;
                    }

                    return new EarlySettlementResponse
                    {
                        Id =
                            earlySettlement.Id,

                        LoanId =
                            loanId,

                        PaymentId =
                            paymentId,

                        LoanBalanceAdjustmentId =
                            adjustment.Id,

                        CompletedInstallments =
                            quote.CompletedInstallments,

                        ContractualBalanceBefore =
                            quote.ContractualBalance,

                        LateFeeBalanceBefore =
                            quote.LateFeeBalance,

                        TotalOutstandingBefore =
                            quote.TotalOutstanding,

                        DiscountType =
                            quote.DiscountType,

                        DiscountValue =
                            quote.DiscountValue,

                        DiscountAmount =
                            quote.DiscountAmount,

                        SettlementAmount =
                            quote.SettlementAmount,

                        AppliedToLateFees =
                            appliedToLateFees,

                        AppliedToLoan =
                            appliedToLoan,

                        ContractualBalanceAfter =
                            contractualBalanceAfter,

                        LateFeeBalanceAfter =
                            lateFeeBalanceAfter,

                        TotalOutstandingAfter =
                            contractualBalanceAfter +
                            lateFeeBalanceAfter,

                        CreatedAt =
                            earlySettlement.CreatedAt
                    };
                },
                cancellationToken);
    }

    public async Task<EarlySettlementResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var settlement =
            await _earlySettlementRepository
                .GetByLoanAsync(
                    tenantId,
                    loanId,
                    cancellationToken);

        if (settlement is null)
        {
            return null;
        }

        /*
        * La prioridad de PaymentService es:
        *
        * 1. Mora
        * 2. Saldo contractual
        *
        * Por tanto podemos reconstruir históricamente
        * la distribución del efectivo usando los snapshots
        * guardados en EarlySettlement.
        */
        var appliedToLateFees =
            Math.Min(
                settlement.SettlementAmount,
                settlement.LateFeeBalanceBefore);

        var appliedToLoan =
            settlement.SettlementAmount
            - appliedToLateFees;

        if (appliedToLoan < 0)
        {
            appliedToLoan = 0;
        }

        return new EarlySettlementResponse
        {
            Id =
                settlement.Id,

            LoanId =
                settlement.LoanId,

            PaymentId =
                settlement.PaymentId,

            LoanBalanceAdjustmentId =
                settlement.LoanBalanceAdjustmentId,

            CompletedInstallments =
                settlement.CompletedInstallments,

            ContractualBalanceBefore =
                settlement.ContractualBalanceBefore,

            LateFeeBalanceBefore =
                settlement.LateFeeBalanceBefore,

            TotalOutstandingBefore =
                settlement.TotalOutstandingBefore,

            DiscountType =
                settlement.DiscountType,

            DiscountValue =
                settlement.DiscountValue,

            DiscountAmount =
                settlement.DiscountAmount,

            SettlementAmount =
                settlement.SettlementAmount,

            AppliedToLateFees =
                appliedToLateFees,

            AppliedToLoan =
                appliedToLoan,

            /*
            * Una liquidación solamente se persiste después
            * de verificar que ambas deudas quedaron en cero.
            *
            * Estos valores representan el resultado histórico
            * inmediato de la liquidación, no deuda futura.
            */
            ContractualBalanceAfter =
                0m,

            LateFeeBalanceAfter =
                0m,

            TotalOutstandingAfter =
                0m,

            CreatedAt =
                settlement.CreatedAt
        };
    }

    private static void ValidateExecuteRequest(
        EarlySettlementExecuteRequest request)
    {
        ValidateRequest(
            new EarlySettlementQuoteRequest
            {
                DiscountType =
                    request.DiscountType,

                DiscountValue =
                    request.DiscountValue
            });

        if (request.ExpectedSettlementAmount < 0)
        {
            throw new InvalidOperationException(
                "Expected settlement amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            throw new InvalidOperationException(
                "A reason is required for early settlement.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new InvalidOperationException(
                "Early settlement reason cannot exceed 500 characters.");
        }
    }

    private static decimal CalculateDiscount(
        decimal installmentAmount,
        decimal contractualBalance,
        EarlySettlementQuoteRequest request)
    {
        decimal discountAmount;

        switch (request.DiscountType)
        {
            case EarlySettlementDiscountType.InstallmentWaiver:
                discountAmount =
                    installmentAmount *
                    request.DiscountValue;

                break;

            case EarlySettlementDiscountType.PercentageDiscount:
                discountAmount =
                    contractualBalance *
                    (request.DiscountValue / 100m);

                break;

            default:
                throw new InvalidOperationException(
                    "Early settlement discount type is invalid.");
        }

        /*
         * Trabajamos con moneda a dos decimales.
         */
        discountAmount =
            Math.Round(
                discountAmount,
                2,
                MidpointRounding.AwayFromZero);

        return discountAmount;
    }

    private static void ValidateRequest(
        EarlySettlementQuoteRequest request)
    {
        if (!Enum.IsDefined(
                typeof(EarlySettlementDiscountType),
                request.DiscountType))
        {
            throw new InvalidOperationException(
                "Early settlement discount type is invalid.");
        }

        if (request.DiscountValue <= 0)
        {
            throw new InvalidOperationException(
                "Discount value must be greater than zero.");
        }

        if (
            request.DiscountType ==
                EarlySettlementDiscountType.InstallmentWaiver)
        {
            if (
                request.DiscountValue != 1 &&
                request.DiscountValue != 2 &&
                request.DiscountValue != 3)
            {
                throw new InvalidOperationException(
                    "Installment waiver must be 1, 2, or 3 installments.");
            }
        }

        if (
            request.DiscountType ==
                EarlySettlementDiscountType.PercentageDiscount)
        {
            if (request.DiscountValue > 100)
            {
                throw new InvalidOperationException(
                    "Percentage discount cannot exceed 100%.");
            }
        }
    }
}