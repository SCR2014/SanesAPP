using System.ComponentModel.DataAnnotations;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.LoanGuarantees;

public sealed class LoanGuaranteeFormModel
    : IValidatableObject
{
    [Required]
    public LoanGuaranteeType Type { get; set; } =
        LoanGuaranteeType.IdentificationDocument;

    [Required(
        ErrorMessage =
            "La referencia de la garantía es obligatoria.")]
    [MaxLength(
        150,
        ErrorMessage =
            "La referencia no puede superar 150 caracteres.")]
    public string Reference { get; set; } =
        string.Empty;

    [MaxLength(
        1000,
        ErrorMessage =
            "La descripción no puede superar 1000 caracteres.")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(
                typeof(LoanGuaranteeType),
                Type))
        {
            yield return new ValidationResult(
                "El tipo de garantía no es válido.",
                [nameof(Type)]);
        }

        if (string.IsNullOrWhiteSpace(
                Reference))
        {
            yield return new ValidationResult(
                "La referencia de la garantía es obligatoria.",
                [nameof(Reference)]);
        }
    }

    public CreateLoanGuaranteeRequest
        ToCreateRequest()
    {
        return new CreateLoanGuaranteeRequest
        {
            Type =
                Type,

            Reference =
                Reference.Trim(),

            Description =
                NormalizeOptional(
                    Description)
        };
    }

    public UpdateLoanGuaranteeRequest
        ToUpdateRequest()
    {
        return new UpdateLoanGuaranteeRequest
        {
            Type =
                Type,

            Reference =
                Reference.Trim(),

            Description =
                NormalizeOptional(
                    Description)
        };
    }

    public static LoanGuaranteeFormModel
        FromResponse(
            LoanGuaranteeResponse guarantee)
    {
        ArgumentNullException.ThrowIfNull(
            guarantee);

        return new LoanGuaranteeFormModel
        {
            Type =
                guarantee.Type,

            Reference =
                guarantee.Reference,

            Description =
                guarantee.Description
        };
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
    }
}