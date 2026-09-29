using System.ComponentModel.DataAnnotations;
using Sanes.Application.LateFees.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.LateFees;

public sealed class LateFeeAdjustmentFormModel
{
    public Guid ChargeId { get; set; }

    public LateFeeAdjustmentType AdjustmentType { get; set; }
        = LateFeeAdjustmentType.Waiver;

    [Range(
        typeof(decimal),
        "0.01",
        "79228162514264337593543950335",
        ErrorMessage =
            "El monto del ajuste debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    [Required(
        ErrorMessage =
            "Debes indicar el motivo del ajuste.")]
    [MaxLength(
        500,
        ErrorMessage =
            "El motivo no puede exceder 500 caracteres.")]
    public string Reason { get; set; } =
        string.Empty;

    public LateFeeAdjustmentRequest ToRequest()
    {
        return new LateFeeAdjustmentRequest
        {
            AdjustmentType =
                AdjustmentType,

            Amount =
                Amount,

            Reason =
                Reason.Trim()
        };
    }

    public static LateFeeAdjustmentFormModel ForCharge(
        Guid chargeId,
        decimal outstandingAmount)
    {
        return new LateFeeAdjustmentFormModel
        {
            ChargeId =
                chargeId,

            AdjustmentType =
                LateFeeAdjustmentType.Waiver,

            Amount =
                outstandingAmount
        };
    }
}