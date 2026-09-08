using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class UpdateLoanRequest : IValidatableObject
{
    [Range(0.01, double.MaxValue)]
    public decimal PrincipalAmount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal InstallmentAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalInstallments { get; set; }

    [Required]
    public PaymentFrequency PaymentFrequency { get; set; }

    public DateTime StartDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(PaymentFrequency), PaymentFrequency))
        {
            yield return new ValidationResult(
                "PaymentFrequency is invalid.",
                new[] { nameof(PaymentFrequency) });
        }

        if (
            PrincipalAmount > 0 &&
            InstallmentAmount > 0 &&
            TotalInstallments > 0)
        {
            var totalAmount =
                InstallmentAmount * TotalInstallments;

            if (totalAmount < PrincipalAmount)
            {
                yield return new ValidationResult(
                    "The total amount of all installments cannot be less than the principal amount.",
                    new[]
                    {
                        nameof(InstallmentAmount),
                        nameof(TotalInstallments),
                        nameof(PrincipalAmount)
                    });
            }
        }
    }
}