using System.ComponentModel.DataAnnotations;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.AppUsers;

public sealed class AppUserFormModel
    : IValidatableObject
{
    [Required(
        ErrorMessage = "El nombre es requerido.")]
    [MaxLength(
        150,
        ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    public string Name { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "El nombre de usuario es requerido.")]
    [MaxLength(
        100,
        ErrorMessage = "El nombre de usuario no puede exceder 100 caracteres.")]
    public string Username { get; set; } =
        string.Empty;

    [EmailAddress(
        ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [MaxLength(
        150,
        ErrorMessage = "El correo electrónico no puede exceder 150 caracteres.")]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public AppUserRole Role { get; set; } =
        AppUserRole.Collector;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Role))
        {
            yield return new ValidationResult(
                "El rol seleccionado no es válido.",
                [nameof(Role)]);
        }
    }

    public CreateAppUserRequest ToCreateRequest(
        string password)
    {
        return new CreateAppUserRequest
        {
            Name =
                Name.Trim(),

            Username =
                Username.Trim(),

            Password =
                password,

            Email =
                NormalizeOptional(
                    Email),

            Phone =
                NormalizeOptional(
                    Phone),

            Role =
                Role
        };
    }

    public UpdateAppUserRequest ToUpdateRequest()
    {
        return new UpdateAppUserRequest
        {
            Name =
                Name.Trim(),

            Username =
                Username.Trim(),

            Email =
                NormalizeOptional(
                    Email),

            Phone =
                NormalizeOptional(
                    Phone),

            Role =
                Role
        };
    }

    public static AppUserFormModel FromResponse(
        AppUserResponse appUser)
    {
        return new AppUserFormModel
        {
            Name =
                appUser.Name,

            Username =
                appUser.Username,

            Email =
                appUser.Email,

            Phone =
                appUser.Phone,

            Role =
                appUser.Role
        };
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}