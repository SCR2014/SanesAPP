using System.ComponentModel.DataAnnotations;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.Loans;

public sealed class LoanFormModel
    : IValidatableObject
{
    public bool IsCreateMode { get; set; } = true;

    public Guid InvestorId { get; set; }

    public Guid ClientId { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; } = 13;

    public PaymentFrequency PaymentFrequency { get; set; }
        = PaymentFrequency.Weekly;

    public DateTime StartDate { get; set; }
        = DateTime.Today;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // ============================================================
    // MORA
    // ============================================================

    public bool OverrideLateFeePolicy { get; set; }

    public bool LateFeeEnabled { get; set; }

    public LateFeeCalculationType LateFeeCalculationType { get; set; }
        = LateFeeCalculationType.FixedAmountPerInstallment;

    public decimal LateFeeAmount { get; set; }

    public int LateFeeGraceDays { get; set; }

    // ============================================================
    // GARANTÍA
    // ============================================================

    public decimal? GuaranteeRequiredFromAmount { get; set; }

    public bool IncludeGuarantee { get; set; }

    public LoanGuaranteeType? GuaranteeType { get; set; }

    [MaxLength(150)]
    public string? GuaranteeReference { get; set; }

    [MaxLength(1000)]
    public string? GuaranteeDescription { get; set; }

    public bool RequiresGuarantee =>
        IsCreateMode &&
        GuaranteeRequiredFromAmount.HasValue &&
        PrincipalAmount >=
            GuaranteeRequiredFromAmount.Value;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (IsCreateMode)
        {
            if (InvestorId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Debe seleccionar un inversionista.",
                    [nameof(InvestorId)]);
            }

            if (ClientId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Debe seleccionar un cliente.",
                    [nameof(ClientId)]);
            }
        }

        if (PrincipalAmount <= 0)
        {
            yield return new ValidationResult(
                "El capital debe ser mayor que cero.",
                [nameof(PrincipalAmount)]);
        }

        if (InstallmentAmount <= 0)
        {
            yield return new ValidationResult(
                "El monto de la cuota debe ser mayor que cero.",
                [nameof(InstallmentAmount)]);
        }

        if (TotalInstallments <= 0)
        {
            yield return new ValidationResult(
                "La cantidad de cuotas debe ser mayor que cero.",
                [nameof(TotalInstallments)]);
        }

        if (!Enum.IsDefined(
            typeof(PaymentFrequency),
            PaymentFrequency))
        {
            yield return new ValidationResult(
                "La frecuencia de pago no es válida.",
                [nameof(PaymentFrequency)]);
        }

        if (
            PrincipalAmount > 0 &&
            InstallmentAmount > 0 &&
            TotalInstallments > 0 &&
            InstallmentAmount * TotalInstallments <
                PrincipalAmount)
        {
            yield return new ValidationResult(
                "El total de las cuotas no puede ser menor que el capital prestado.",
                [
                    nameof(PrincipalAmount),
                    nameof(InstallmentAmount),
                    nameof(TotalInstallments)
                ]);
        }

        if (
            IsCreateMode &&
            OverrideLateFeePolicy)
        {
            if (!Enum.IsDefined(
                typeof(LateFeeCalculationType),
                LateFeeCalculationType))
            {
                yield return new ValidationResult(
                    "El tipo de cálculo de mora no es válido.",
                    [nameof(LateFeeCalculationType)]);
            }

            if (LateFeeAmount < 0)
            {
                yield return new ValidationResult(
                    "El monto de mora no puede ser negativo.",
                    [nameof(LateFeeAmount)]);
            }

            if (LateFeeGraceDays < 0)
            {
                yield return new ValidationResult(
                    "Los días de gracia no pueden ser negativos.",
                    [nameof(LateFeeGraceDays)]);
            }

            if (
                LateFeeEnabled &&
                LateFeeAmount <= 0)
            {
                yield return new ValidationResult(
                    "Cuando la mora está habilitada, el monto debe ser mayor que cero.",
                    [nameof(LateFeeAmount)]);
            }
        }

        if (
            IsCreateMode &&
            RequiresGuarantee &&
            !IncludeGuarantee)
        {
            yield return new ValidationResult(
                "Este préstamo requiere una garantía por el monto del capital.",
                [nameof(IncludeGuarantee)]);
        }

        if (
            IsCreateMode &&
            (IncludeGuarantee ||
             RequiresGuarantee))
        {
            if (!GuaranteeType.HasValue)
            {
                yield return new ValidationResult(
                    "Debe seleccionar el tipo de garantía.",
                    [nameof(GuaranteeType)]);
            }
            else if (!Enum.IsDefined(
                typeof(LoanGuaranteeType),
                GuaranteeType.Value))
            {
                yield return new ValidationResult(
                    "El tipo de garantía no es válido.",
                    [nameof(GuaranteeType)]);
            }

            if (string.IsNullOrWhiteSpace(
                GuaranteeReference))
            {
                yield return new ValidationResult(
                    "La referencia de la garantía es requerida.",
                    [nameof(GuaranteeReference)]);
            }
        }
    }

    public CreateLoanRequest ToCreateRequest()
    {
        return new CreateLoanRequest
        {
            InvestorId =
                InvestorId,

            ClientId =
                ClientId,

            PrincipalAmount =
                PrincipalAmount,

            InstallmentAmount =
                InstallmentAmount,

            TotalInstallments =
                TotalInstallments,

            PaymentFrequency =
                PaymentFrequency,

            StartDate =
                StartDate,

            Notes =
                Notes,

            LateFeeEnabled =
                OverrideLateFeePolicy
                    ? LateFeeEnabled
                    : null,

            LateFeeCalculationType =
                OverrideLateFeePolicy
                    ? LateFeeCalculationType
                    : null,

            LateFeeAmount =
                OverrideLateFeePolicy
                    ? LateFeeAmount
                    : null,

            LateFeeGraceDays =
                OverrideLateFeePolicy
                    ? LateFeeGraceDays
                    : null,

            Guarantee =
                IncludeGuarantee ||
                RequiresGuarantee
                    ? new CreateLoanGuaranteeRequest
                    {
                        Type =
                            GuaranteeType
                            ?? default,

                        Reference =
                            GuaranteeReference
                            ?? string.Empty,

                        Description =
                            GuaranteeDescription
                    }
                    : null
        };
    }

    public UpdateLoanRequest ToUpdateRequest()
    {
        return new UpdateLoanRequest
        {
            PrincipalAmount =
                PrincipalAmount,

            InstallmentAmount =
                InstallmentAmount,

            TotalInstallments =
                TotalInstallments,

            PaymentFrequency =
                PaymentFrequency,

            StartDate =
                StartDate,

            Notes =
                Notes
        };
    }

    public static LoanFormModel ForCreate(
        TenantDto tenant)
    {
        return new LoanFormModel
        {
            IsCreateMode =
                true,

            TotalInstallments =
                13,

            PaymentFrequency =
                PaymentFrequency.Weekly,

            StartDate =
                DateTime.Today,

            OverrideLateFeePolicy =
                false,

            LateFeeEnabled =
                tenant.DefaultLateFeeEnabled,

            LateFeeCalculationType =
                tenant.DefaultLateFeeCalculationType,

            LateFeeAmount =
                tenant.DefaultLateFeeAmount,

            LateFeeGraceDays =
                tenant.DefaultLateFeeGraceDays,

            GuaranteeRequiredFromAmount =
                tenant.GuaranteeRequiredFromAmount
        };
    }

    public static LoanFormModel FromResponse(
        LoanResponse loan)
    {
        return new LoanFormModel
        {
            IsCreateMode =
                false,

            InvestorId =
                loan.InvestorId,

            ClientId =
                loan.ClientId,

            PrincipalAmount =
                loan.PrincipalAmount,

            InstallmentAmount =
                loan.InstallmentAmount,

            TotalInstallments =
                loan.TotalInstallments,

            PaymentFrequency =
                loan.PaymentFrequency,

            StartDate =
                loan.StartDate.Date,

            Notes =
                loan.Notes,

            LateFeeEnabled =
                loan.LateFeeEnabled,

            LateFeeCalculationType =
                loan.LateFeeCalculationType,

            LateFeeAmount =
                loan.LateFeeAmount,

            LateFeeGraceDays =
                loan.LateFeeGraceDays,

            GuaranteeRequiredFromAmount =
                loan.GuaranteeThresholdAtCreation,

            IncludeGuarantee =
                loan.Guarantee is not null,

            GuaranteeType =
                loan.Guarantee?.Type,

            GuaranteeReference =
                loan.Guarantee?.Reference,

            GuaranteeDescription =
                loan.Guarantee?.Description
        };
    }
}