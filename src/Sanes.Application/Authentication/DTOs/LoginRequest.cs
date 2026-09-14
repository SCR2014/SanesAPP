using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Authentication.DTOs;

public class LoginRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}