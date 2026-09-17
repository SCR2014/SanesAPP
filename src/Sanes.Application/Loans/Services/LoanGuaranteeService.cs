using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.Services;

public class LoanGuaranteeService
    : ILoanGuaranteeService
{
    private readonly ILoanRepository
        _loanRepository;

    public LoanGuaranteeService(
        ILoanRepository loanRepository)
    {
        _loanRepository =
            loanRepository;
    }

    public async Task<LoanGuaranteeResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan =
            await _loanRepository.GetByIdAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null ||
            loan.Guarantee is null)
        {
            return null;
        }

        return Map(
            loan.Guarantee);
    }

    public async Task<LoanGuaranteeResponse?> CreateAsync(
        Guid tenantId,
        Guid loanId,
        CreateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateGuarantee(
            request.Type,
            request.Reference,
            request.Description);

        var loan =
            await _loanRepository.GetByIdForUpdateAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null)
        {
            return null;
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Guarantees can only be added to active loans.");
        }

        if (loan.Guarantee is not null)
        {
            throw new InvalidOperationException(
                "The loan already has a guarantee.");
        }

        var now =
            DateTime.UtcNow;

        var guarantee =
            new LoanGuarantee
            {
                TenantId =
                    tenantId,

                LoanId =
                    loan.Id,

                Loan =
                    loan,

                Type =
                    request.Type,

                Reference =
                    request.Reference.Trim(),

                Description =
                    NormalizeOptional(
                        request.Description),

                CreatedAt =
                    now,

                UpdatedAt =
                    now
            };

        loan.Guarantee =
            guarantee;

        loan.UpdatedAt =
            now;

        /*
        * El Loan ya existe y está tracked.
        *
        * Registramos explícitamente la nueva garantía
        * como Added para que EF genere un INSERT.
        */
        await _loanRepository.AddGuaranteeAsync(
            guarantee,
            cancellationToken);

        await _loanRepository.SaveChangesAsync(
            cancellationToken);

        return Map(
            guarantee);
    }

    public async Task<LoanGuaranteeResponse?> UpdateAsync(
        Guid tenantId,
        Guid loanId,
        UpdateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateGuarantee(
            request.Type,
            request.Reference,
            request.Description);

        var loan =
            await _loanRepository.GetByIdForUpdateAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null)
        {
            return null;
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Guarantees can only be updated for active loans.");
        }

        if (loan.Guarantee is null)
        {
            throw new InvalidOperationException(
                "The loan does not have a guarantee.");
        }

        var now =
            DateTime.UtcNow;

        loan.Guarantee.Type =
            request.Type;

        loan.Guarantee.Reference =
            request.Reference.Trim();

        loan.Guarantee.Description =
            NormalizeOptional(
                request.Description);

        loan.Guarantee.UpdatedAt =
            now;

        loan.UpdatedAt =
            now;

        await _loanRepository.SaveChangesAsync(
            cancellationToken);

        return Map(
            loan.Guarantee);
    }

    private static void ValidateGuarantee(
        LoanGuaranteeType type,
        string reference,
        string? description)
    {
        if (!Enum.IsDefined(
                typeof(LoanGuaranteeType),
                type))
        {
            throw new InvalidOperationException(
                "Guarantee type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                reference))
        {
            throw new InvalidOperationException(
                "Guarantee reference is required.");
        }

        if (reference.Trim().Length > 150)
        {
            throw new InvalidOperationException(
                "Guarantee reference cannot exceed 150 characters.");
        }

        if (
            description is not null &&
            description.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "Guarantee description cannot exceed 1000 characters.");
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static LoanGuaranteeResponse Map(
        LoanGuarantee guarantee)
    {
        return new LoanGuaranteeResponse
        {
            Id =
                guarantee.Id,

            Type =
                guarantee.Type,

            Reference =
                guarantee.Reference,

            Description =
                guarantee.Description,

            CreatedAt =
                guarantee.CreatedAt,

            UpdatedAt =
                guarantee.UpdatedAt
        };
    }
}