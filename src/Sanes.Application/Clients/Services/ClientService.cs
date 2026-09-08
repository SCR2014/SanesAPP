using Sanes.Application.Clients.DTOs;
using Sanes.Application.Clients.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Domain.Entities;

namespace Sanes.Application.Clients.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly ITenantRepository _tenantRepository;

    public ClientService(
        IClientRepository clientRepository,
        ITenantRepository tenantRepository)
    {
        _clientRepository = clientRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            request.TenantId,
            cancellationToken);

        if (tenant is null)
        {
            throw new InvalidOperationException(
                "Tenant not found or inactive.");
        }

        var identification = NormalizeOptional(request.Identification);

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var exists = await _clientRepository.ExistsByIdentificationAsync(
                request.TenantId,
                identification,
                cancellationToken: cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    "A client with the same identification already exists for this tenant.");
            }
        }

        var client = new Client
        {
            TenantId = request.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = NormalizeOptional(request.LastName),
            Phone = request.Phone.Trim(),
            SecondaryPhone = NormalizeOptional(request.SecondaryPhone),
            IdentificationType = NormalizeOptional(request.IdentificationType),
            Identification = identification,
            SocialNumber = NormalizeOptional(request.SocialNumber),
            Address = NormalizeOptional(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Notes = NormalizeOptional(request.Notes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _clientRepository.AddAsync(client, cancellationToken);
        await _clientRepository.SaveChangesAsync(cancellationToken);

        return Map(client);
    }

    public async Task<List<ClientResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.GetAllAsync(
            tenantId,
            cancellationToken);

        return clients
            .Select(Map)
            .ToList();
    }

    public async Task<ClientResponse?> GetByIdAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(
            tenantId,
            clientId,
            cancellationToken);

        return client is null
            ? null
            : Map(client);
    }

    public async Task<ClientResponse?> UpdateAsync(
        Guid tenantId,
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdForUpdateAsync(
            tenantId,
            clientId,
            cancellationToken);

        if (client is null || !client.IsActive)
        {
            return null;
        }

        var identification = NormalizeOptional(request.Identification);

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var exists = await _clientRepository.ExistsByIdentificationAsync(
                tenantId,
                identification,
                clientId,
                cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    "A client with the same identification already exists for this tenant.");
            }
        }

        client.FirstName = request.FirstName.Trim();
        client.LastName = NormalizeOptional(request.LastName);
        client.Phone = request.Phone.Trim();
        client.SecondaryPhone = NormalizeOptional(request.SecondaryPhone);
        client.IdentificationType = NormalizeOptional(request.IdentificationType);
        client.Identification = identification;
        client.SocialNumber = NormalizeOptional(request.SocialNumber);
        client.Address = NormalizeOptional(request.Address);
        client.Latitude = request.Latitude;
        client.Longitude = request.Longitude;
        client.Notes = NormalizeOptional(request.Notes);
        client.UpdatedAt = DateTime.UtcNow;

        await _clientRepository.SaveChangesAsync(cancellationToken);

        return Map(client);
    }

    public async Task<bool> DeleteAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdForUpdateAsync(
            tenantId,
            clientId,
            cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;

        await _clientRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ReactivateAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdForUpdateAsync(
            tenantId,
            clientId,
            cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.IsActive = true;
        client.UpdatedAt = DateTime.UtcNow;

        await _clientRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static ClientResponse Map(Client client)
    {
        return new ClientResponse
        {
            Id = client.Id,
            TenantId = client.TenantId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Phone = client.Phone,
            SecondaryPhone = client.SecondaryPhone,
            IdentificationType = client.IdentificationType,
            Identification = client.Identification,
            SocialNumber = client.SocialNumber,
            Address = client.Address,
            Latitude = client.Latitude,
            Longitude = client.Longitude,
            Notes = client.Notes,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}