using System.ComponentModel.DataAnnotations;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.TenantSettings;

public sealed class TenantSettingsFormModel
    : IValidatableObject
{
    [Required(
        ErrorMessage = "El nombre comercial es requerido.")]
    [MaxLength(
        150,
        ErrorMessage = "El nombre comercial no puede exceder 150 caracteres.")]
    public string Name { get; set; } =
        string.Empty;

    [MaxLength(
        200,
        ErrorMessage = "La razón social no puede exceder 200 caracteres.")]
    public string? LegalName { get; set; }

    [MaxLength(
        30,
        ErrorMessage = "El teléfono no puede exceder 30 caracteres.")]
    public string? Phone { get; set; }

    [EmailAddress(
        ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [MaxLength(
        150,
        ErrorMessage = "El correo electrónico no puede exceder 150 caracteres.")]
    public string? Email { get; set; }

    [Required(
        ErrorMessage = "El código de moneda es requerido.")]
    [StringLength(
        3,
        MinimumLength = 3,
        ErrorMessage = "El código de moneda debe tener exactamente 3 caracteres.")]
    public string CurrencyCode { get; set; } =
        "USD";

    [Required(
        ErrorMessage = "El símbolo de moneda es requerido.")]
    [MaxLength(
        10,
        ErrorMessage = "El símbolo de moneda no puede exceder 10 caracteres.")]
    public string CurrencySymbol { get; set; } =
        "$";

    public bool DefaultLateFeeEnabled { get; set; }

    public decimal DefaultLateFeeAmount { get; set; }

    public int DefaultLateFeeGraceDays { get; set; }

    public decimal? GuaranteeRequiredFromAmount { get; set; }

    public static TenantSettingsFormModel FromDto(
        TenantDto tenant)
    {
        ArgumentNullException.ThrowIfNull(
            tenant);

        return new TenantSettingsFormModel
        {
            Name =
                tenant.Name,

            LegalName =
                tenant.LegalName,

            Phone =
                tenant.Phone,

            Email =
                tenant.Email,

            CurrencyCode =
                tenant.CurrencyCode,

            CurrencySymbol =
                tenant.CurrencySymbol,

            DefaultLateFeeEnabled =
                tenant.DefaultLateFeeEnabled,

            DefaultLateFeeAmount =
                tenant.DefaultLateFeeAmount,

            DefaultLateFeeGraceDays =
                tenant.DefaultLateFeeGraceDays,

            GuaranteeRequiredFromAmount =
                tenant.GuaranteeRequiredFromAmount
        };
    }

    public UpdateTenantRequest ToUpdateRequest()
    {
        return new UpdateTenantRequest
        {
            Name =
                Name.Trim(),

            LegalName =
                NormalizeOptional(
                    LegalName),

            Phone =
                NormalizeOptional(
                    Phone),

            Email =
                NormalizeOptional(
                    Email),

            CurrencyCode =
                CurrencyCode
                    .Trim()
                    .ToUpperInvariant(),

            CurrencySymbol =
                CurrencySymbol.Trim(),

            DefaultLateFeeEnabled =
                DefaultLateFeeEnabled,

            DefaultLateFeeCalculationType =
                LateFeeCalculationType
                    .FixedAmountPerInstallment,

            DefaultLateFeeAmount =
                DefaultLateFeeAmount,

            DefaultLateFeeGraceDays =
                DefaultLateFeeGraceDays,

            GuaranteeRequiredFromAmount =
                GuaranteeRequiredFromAmount
        };
    }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(
                Name))
        {
            yield return new ValidationResult(
                "El nombre comercial no puede estar vacío.",
                [nameof(Name)]);
        }

        if (string.IsNullOrWhiteSpace(
                CurrencyCode))
        {
            yield return new ValidationResult(
                "El código de moneda no puede estar vacío.",
                [nameof(CurrencyCode)]);
        }

        if (string.IsNullOrWhiteSpace(
                CurrencySymbol))
        {
            yield return new ValidationResult(
                "El símbolo de moneda no puede estar vacío.",
                [nameof(CurrencySymbol)]);
        }

        if (DefaultLateFeeAmount < 0)
        {
            yield return new ValidationResult(
                "El monto de mora no puede ser negativo.",
                [nameof(DefaultLateFeeAmount)]);
        }

        if (DefaultLateFeeGraceDays < 0)
        {
            yield return new ValidationResult(
                "Los días de gracia no pueden ser negativos.",
                [nameof(DefaultLateFeeGraceDays)]);
        }

        if (DefaultLateFeeEnabled &&
            DefaultLateFeeAmount <= 0)
        {
            yield return new ValidationResult(
                "El monto de mora debe ser mayor que cero cuando la mora está habilitada.",
                [nameof(DefaultLateFeeAmount)]);
        }

        if (GuaranteeRequiredFromAmount.HasValue &&
            GuaranteeRequiredFromAmount.Value <= 0)
        {
            yield return new ValidationResult(
                "El monto mínimo para exigir garantía debe ser mayor que cero.",
                [nameof(GuaranteeRequiredFromAmount)]);
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        return value.Trim();
    }
}