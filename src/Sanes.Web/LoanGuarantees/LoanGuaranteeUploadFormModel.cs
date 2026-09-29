using System.ComponentModel.DataAnnotations;

namespace Sanes.Web.LoanGuarantees;

public sealed class LoanGuaranteeUploadFormModel
{
    [MaxLength(
        1000,
        ErrorMessage =
            "La descripción no puede superar 1000 caracteres.")]
    public string? Description { get; set; }

    public string? GetNormalizedDescription()
    {
        return string.IsNullOrWhiteSpace(
                Description)
            ? null
            : Description.Trim();
    }
}