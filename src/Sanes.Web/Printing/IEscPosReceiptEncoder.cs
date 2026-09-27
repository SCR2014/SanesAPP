using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.Printing;

public interface IEscPosReceiptEncoder
{
    byte[] Encode(
        PaymentReceiptResponse receipt);
}