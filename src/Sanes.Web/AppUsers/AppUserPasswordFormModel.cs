using System.ComponentModel.DataAnnotations;

namespace Sanes.Web.AppUsers;

public sealed class AppUserPasswordFormModel
    : IValidatableObject
{
    [Required(
        ErrorMessage = "La contraseña es requerida.")]
    [MinLength(
        8,
        ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(
        100,
        ErrorMessage = "La contraseña no puede exceder 100 caracteres.")]
    public string Password { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "Debe confirmar la contraseña.")]
    public string ConfirmPassword { get; set; } =
        string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.Equals(
                Password,
                ConfirmPassword,
                StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Las contraseñas no coinciden.",
                [nameof(ConfirmPassword)]);
        }
    }
}