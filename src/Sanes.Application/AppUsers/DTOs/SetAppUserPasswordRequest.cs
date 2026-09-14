using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.AppUsers.DTOs;

public class SetAppUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}