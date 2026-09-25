using System.ComponentModel.DataAnnotations;
using Sanes.Application.Clients.DTOs;

namespace Sanes.Web.Clients;

public sealed class ClientFormModel
    : IValidatableObject
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } =
        string.Empty;

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    [MaxLength(30)]
    public string Phone { get; set; } =
        string.Empty;

    [MaxLength(30)]
    public string? SecondaryPhone { get; set; }

    [MaxLength(50)]
    public string? IdentificationType { get; set; }

    [MaxLength(100)]
    public string? Identification { get; set; }

    [MaxLength(150)]
    public string? SocialNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public Guid? CollectionRouteId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(
            FirstName))
        {
            yield return new ValidationResult(
                "El nombre es requerido.",
                [nameof(FirstName)]);
        }

        if (string.IsNullOrWhiteSpace(
            Phone))
        {
            yield return new ValidationResult(
                "El teléfono es requerido.",
                [nameof(Phone)]);
        }

        if (Latitude is < -90 or > 90)
        {
            yield return new ValidationResult(
                "La latitud debe estar entre -90 y 90.",
                [nameof(Latitude)]);
        }

        if (Longitude is < -180 or > 180)
        {
            yield return new ValidationResult(
                "La longitud debe estar entre -180 y 180.",
                [nameof(Longitude)]);
        }
    }

    public CreateClientRequest ToCreateRequest()
    {
        return new CreateClientRequest
        {
            FirstName = FirstName,
            LastName = LastName,
            Phone = Phone,
            SecondaryPhone = SecondaryPhone,
            IdentificationType =
                IdentificationType,
            Identification =
                Identification,
            SocialNumber =
                SocialNumber,
            Address =
                Address,
            Latitude =
                Latitude,
            Longitude =
                Longitude,
            CollectionRouteId =
                CollectionRouteId,
            Notes =
                Notes
        };
    }

    public UpdateClientRequest ToUpdateRequest()
    {
        return new UpdateClientRequest
        {
            FirstName = FirstName,
            LastName = LastName,
            Phone = Phone,
            SecondaryPhone = SecondaryPhone,
            IdentificationType =
                IdentificationType,
            Identification =
                Identification,
            SocialNumber =
                SocialNumber,
            Address =
                Address,
            Latitude =
                Latitude,
            Longitude =
                Longitude,
            CollectionRouteId =
                CollectionRouteId,
            Notes =
                Notes
        };
    }

    public static ClientFormModel FromResponse(
        ClientResponse client)
    {
        return new ClientFormModel
        {
            FirstName =
                client.FirstName,
            LastName =
                client.LastName,
            Phone =
                client.Phone,
            SecondaryPhone =
                client.SecondaryPhone,
            IdentificationType =
                client.IdentificationType,
            Identification =
                client.Identification,
            SocialNumber =
                client.SocialNumber,
            Address =
                client.Address,
            Latitude =
                client.Latitude,
            Longitude =
                client.Longitude,
            CollectionRouteId =
                client.CollectionRouteId,
            Notes =
                client.Notes
        };
    }
}