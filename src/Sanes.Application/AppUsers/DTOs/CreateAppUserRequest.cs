using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.AppUsers.DTOs;

public class CreateAppUserRequest
{
    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public AppUserRole Role { get; set; }
}