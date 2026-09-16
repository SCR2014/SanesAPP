using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class CreateLoanRequest : IValidatableObject
{
    [Required]
    public Guid InvestorId { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal PrincipalAmount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal InstallmentAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalInstallments { get; set; }

    [Required]
    public PaymentFrequency PaymentFrequency { get; set; }

    public bool? LateFeeEnabled { get; set; }

    public LateFeeCalculationType? LateFeeCalculationType { get; set; }

    public decimal? LateFeeAmount { get; set; }

    public int? LateFeeGraceDays { get; set; }

    public DateTime StartDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (InvestorId == Guid.Empty)
        {
            yield return new ValidationResult(
                "InvestorId must be a valid identifier.",
                new[] { nameof(InvestorId) });
        }

        if (ClientId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ClientId must be a valid identifier.",
                new[] { nameof(ClientId) });
        }

        if (!Enum.IsDefined(typeof(PaymentFrequency), PaymentFrequency))
        {
            yield return new ValidationResult(
                "PaymentFrequency is invalid.",
                new[] { nameof(PaymentFrequency) });
        }

        if (
            LateFeeCalculationType.HasValue &&
            !Enum.IsDefined(
                typeof(LateFeeCalculationType),
                LateFeeCalculationType.Value))
        {
            yield return new ValidationResult(
                "LateFeeCalculationType is invalid.",
                new[] { nameof(LateFeeCalculationType) });
        }

        if (LateFeeAmount.HasValue && LateFeeAmount.Value < 0)
        {
            yield return new ValidationResult(
                "LateFeeAmount cannot be negative.",
                new[] { nameof(LateFeeAmount) });
        }

        if (LateFeeGraceDays.HasValue && LateFeeGraceDays.Value < 0)
        {
            yield return new ValidationResult(
                "LateFeeGraceDays cannot be negative.",
                new[] { nameof(LateFeeGraceDays) });
        }

        if (
            LateFeeEnabled == true &&
            LateFeeCalculationType ==
                Sanes.Domain.Enums.LateFeeCalculationType.FixedAmountPerInstallment &&
            LateFeeAmount.HasValue &&
            LateFeeAmount.Value <= 0)
        {
            yield return new ValidationResult(
                "LateFeeAmount must be greater than zero when fixed late fees are enabled.",
                new[] { nameof(LateFeeAmount) });
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