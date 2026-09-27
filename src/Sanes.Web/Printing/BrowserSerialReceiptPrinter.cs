using Microsoft.JSInterop;
using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.Printing;

public sealed class BrowserSerialReceiptPrinter
    : IReceiptPrinter
{
    private readonly IJSRuntime _js;
    private readonly IEscPosReceiptEncoder
        _encoder;

    public BrowserSerialReceiptPrinter(
        IJSRuntime js,
        IEscPosReceiptEncoder encoder)
    {
        _js = js;
        _encoder = encoder;
    }

    public async Task PrintAsync(
        PaymentReceiptResponse receipt,
        CancellationToken cancellationToken = default)
    {
        var data =
            _encoder.Encode(
                receipt);

        await _js.InvokeVoidAsync(
            "SanesSerialPrinter.printBytes",
            cancellationToken,
            data);
    }
}