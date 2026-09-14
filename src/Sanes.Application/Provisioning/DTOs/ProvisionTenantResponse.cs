namespace Sanes.Application.Provisioning.DTOs;

public class ProvisionTenantResponse
{
    public Guid TenantId { get; set; }

    public string TenantName { get; set; } = string.Empty;

    public Guid AdministratorId { get; set; }

    public string AdministratorName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}