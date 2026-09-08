using Sanes.Application.Investors.DTOs;
using Sanes.Application.Investors.Repositories;
using Sanes.Domain.Entities;
using Sanes.Application.Tenants.Repositories;

namespace Sanes.Application.Investors.Services;

public class InvestorService : IInvestorService
{
    private readonly IInvestorRepository _investorRepository;
    private readonly ITenantRepository _tenantRepository;

    public InvestorService(
        IInvestorRepository investorRepository,
        ITenantRepository tenantRepository)
    {
        _investorRepository = investorRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<InvestorResponse> CreateAsync(
        CreateInvestorRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            request.TenantId,
            cancellationToken);

        if (tenant is null)
        {
            throw new InvalidOperationException(
                "The tenant does not exist or is inactive.");
        }

        var name = request.Name.Trim();
        var phone = request.Phone?.Trim();
        var email = request.Email?.Trim();
        var identification = request.Identification?.Trim();
        var notes = request.Notes?.Trim();

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var identificationExists =
                await _investorRepository.ExistsByIdentificationAsync(
                    request.TenantId,
                    identification,
                    cancellationToken: cancellationToken);

            if (identificationExists)
            {
                throw new InvalidOperationException(
                    "An investor with the same identification already exists for this tenant.");
            }
        }

        var investor = new Investor
        {
            TenantId = request.TenantId,
            Name = name,
            Phone = phone,
            Email = email,
            Identification = identification,
            Notes = notes
        };

        await _investorRepository.AddAsync(
            investor,
            cancellationToken);

        await _investorRepository.SaveChangesAsync(
            cancellationToken);

        return new InvestorResponse
        {
            Id = investor.Id,
            TenantId = investor.TenantId,
            Name = investor.Name,
            Phone = investor.Phone,
            Email = investor.Email,
            Identification = investor.Identification,
            Notes = investor.Notes,
            IsActive = investor.IsActive,
            CreatedAt = investor.CreatedAt,
            UpdatedAt = investor.UpdatedAt
        };
    }

    public async Task<List<InvestorResponse>> GetAllAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
{
    var investors = await _investorRepository.GetAllAsync(
        tenantId,
        cancellationToken);

    return investors
        .Select(investor => new InvestorResponse
        {
            Id = investor.Id,
            TenantId = investor.TenantId,
            Name = investor.Name,
            Phone = investor.Phone,
            Email = investor.Email,
            Identification = investor.Identification,
            Notes = investor.Notes,
            IsActive = investor.IsActive,
            CreatedAt = investor.CreatedAt,
            UpdatedAt = investor.UpdatedAt
        })
        .ToList();
}

    public async Task<InvestorResponse?> GetByIdAsync(
    Guid tenantId,
    Guid investorId,
    CancellationToken cancellationToken = default)
{
    var investor = await _investorRepository.GetByIdAsync(
        tenantId,
        investorId,
        cancellationToken);

    if (investor is null)
    {
        return null;
    }

    return new InvestorResponse
    {
        Id = investor.Id,
        TenantId = investor.TenantId,
        Name = investor.Name,
        Phone = investor.Phone,
        Email = investor.Email,
        Identification = investor.Identification,
        Notes = investor.Notes,
        IsActive = investor.IsActive,
        CreatedAt = investor.CreatedAt,
        UpdatedAt = investor.UpdatedAt
    };
}

    public async Task<InvestorResponse?> UpdateAsync(
    Guid tenantId,
    Guid investorId,
    UpdateInvestorRequest request,
    CancellationToken cancellationToken = default)
{
    var investor = await _investorRepository.GetByIdForUpdateAsync(
        tenantId,
        investorId,
        cancellationToken);

    if (investor is null || !investor.IsActive)
    {
        return null;
    }

    var name = request.Name.Trim();
    var phone = request.Phone?.Trim();
    var email = request.Email?.Trim();
    var identification = request.Identification?.Trim();
    var notes = request.Notes?.Trim();

    if (!string.IsNullOrWhiteSpace(identification))
    {
        var identificationExists =
            await _investorRepository.ExistsByIdentificationAsync(
                tenantId,
                identification,
                investorId,
                cancellationToken);

        if (identificationExists)
        {
            throw new InvalidOperationException(
                "An investor with the same identification already exists for this tenant.");
        }
    }

    investor.Name = name;
    investor.Phone = phone;
    investor.Email = email;
    investor.Identification = identification;
    investor.Notes = notes;
    investor.UpdatedAt = DateTime.UtcNow;

    await _investorRepository.SaveChangesAsync(
        cancellationToken);

    return new InvestorResponse
    {
        Id = investor.Id,
        TenantId = investor.TenantId,
        Name = investor.Name,
        Phone = investor.Phone,
        Email = investor.Email,
        Identification = investor.Identification,
        Notes = investor.Notes,
        IsActive = investor.IsActive,
        CreatedAt = investor.CreatedAt,
        UpdatedAt = investor.UpdatedAt
    };
}

    public async Task<bool> DeleteAsync(
    Guid tenantId,
    Guid investorId,
    CancellationToken cancellationToken = default)
    {
        var investor = await _investorRepository.GetByIdForUpdateAsync(
            tenantId,
            investorId,
            cancellationToken);

        if (investor is null)
        {
            return false;
        }

        investor.IsActive = false;
        investor.UpdatedAt = DateTime.UtcNow;

        await _investorRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> ReactivateAsync(
    Guid tenantId,
    Guid investorId,
    CancellationToken cancellationToken = default)
    {
        var investor = await _investorRepository.GetByIdForUpdateAsync(
            tenantId,
            investorId,
            cancellationToken);

        if (investor is null)
        {
            return false;
        }

        investor.IsActive = true;
        investor.UpdatedAt = DateTime.UtcNow;

        await _investorRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}