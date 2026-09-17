using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class LoanGuaranteeResponse
{
    public Guid Id { get; set; }

    public LoanGuaranteeType Type { get; set; }

    public string Reference { get; set; }
        = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}