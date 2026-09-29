using System.Globalization;
using System.Text;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.Printing;

public sealed class EscPosReceiptEncoder
    : IEscPosReceiptEncoder
{
    private const int CharactersPerLine = 32;

    public byte[] Encode(
        PaymentReceiptResponse receipt)
    {
        ArgumentNullException.ThrowIfNull(
            receipt);

        using var stream =
            new MemoryStream();

        // ESC @ - Inicializar
        WriteCommand(
            stream,
            0x1B,
            0x40);

        // Centrado
        SetAlignment(
            stream,
            1);

        SetBold(
            stream,
            true);

        WriteWrapped(
            stream,
            receipt.TenantName);

        SetBold(
            stream,
            false);

        if (!string.IsNullOrWhiteSpace(
                receipt.TenantLegalName))
        {
            WriteWrapped(
                stream,
                receipt.TenantLegalName);
        }

        WriteLine(
            stream,
            string.Empty);

        SetBold(
            stream,
            true);

        WriteLine(
            stream,
            "RECIBO DE PAGO");

        SetBold(
            stream,
            false);

        WriteLine(
            stream,
            receipt.ReceiptNumber);

        WriteSeparator(
            stream);

        // Alineación izquierda
        SetAlignment(
            stream,
            0);

        WriteWrapped(
            stream,
            $"Fecha: {receipt.PaymentDate.ToLocalTime():dd/MM/yyyy HH:mm}");

        WriteWrapped(
            stream,
            $"Cliente: {receipt.ClientName}");

        WriteWrapped(
            stream,
            $"Tipo: {GetPaymentTypeLabel(receipt.PaymentType)}");

        WriteSeparator(
            stream);

        WriteAmountLine(
            stream,
            "Monto recibido",
            FormatMoney(
                receipt,
                receipt.AmountReceived));

        WriteAmountLine(
            stream,
            "Aplicado a mora",
            FormatMoney(
                receipt,
                receipt.LateFeeAmountApplied));

        WriteAmountLine(
            stream,
            "Aplicado prestamo",
            FormatMoney(
                receipt,
                receipt.LoanBalanceAmountApplied));

        WriteSeparator(
            stream);

        WriteAmountLine(
            stream,
            "Saldo contractual",
            FormatMoney(
                receipt,
                receipt.ContractualBalanceAfter));

        WriteAmountLine(
            stream,
            "Mora pendiente",
            FormatMoney(
                receipt,
                receipt.LateFeeBalanceAfter));

        SetBold(
            stream,
            true);

        WriteAmountLine(
            stream,
            "TOTAL PENDIENTE",
            FormatMoney(
                receipt,
                receipt.TotalOutstandingAfter));

        SetBold(
            stream,
            false);

        WriteSeparator(
            stream);

        WriteWrapped(
            stream,
            $"Cobrador: " +
            $"{receipt.CollectedByName ?? "No especificado"}");

        if (!string.IsNullOrWhiteSpace(
                receipt.Notes))
        {
            WriteWrapped(
                stream,
                $"Notas: {receipt.Notes}");
        }

        WriteSeparator(
            stream);

        SetAlignment(
            stream,
            1);

        WriteLine(
            stream,
            "Gracias por su pago");

        WriteLine(
            stream,
            receipt.ReceiptNumber);

        WriteLine(
            stream,
            string.Empty);

        // ESC d 4 - avanzar papel
        WriteCommand(
            stream,
            0x1B,
            0x64,
            0x04);

        return stream.ToArray();
    }

    private static void SetAlignment(
        Stream stream,
        byte alignment)
    {
        WriteCommand(
            stream,
            0x1B,
            0x61,
            alignment);
    }

    private static void SetBold(
        Stream stream,
        bool enabled)
    {
        WriteCommand(
            stream,
            0x1B,
            0x45,
            enabled
                ? (byte)1
                : (byte)0);
    }

    private static void WriteSeparator(
        Stream stream)
    {
        WriteLine(
            stream,
            new string(
                '-',
                CharactersPerLine));
    }

    private static void WriteAmountLine(
        Stream stream,
        string label,
        string value)
    {
        label = ToAscii(
            label);

        value = ToAscii(
            value);

        var spaces =
            CharactersPerLine -
            label.Length -
            value.Length;

        if (spaces >= 1)
        {
            WriteLine(
                stream,
                label +
                new string(
                    ' ',
                    spaces) +
                value);

            return;
        }

        WriteLine(
            stream,
            label);

        WriteLine(
            stream,
            value.PadLeft(
                CharactersPerLine));
    }

    private static void WriteWrapped(
        Stream stream,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return;
        }

        foreach (var line in Wrap(
                     value))
        {
            WriteLine(
                stream,
                line);
        }
    }

    private static IEnumerable<string> Wrap(
        string value)
    {
        var remaining =
            ToAscii(
                    value)
                .Replace(
                    "\r",
                    " ")
                .Replace(
                    "\n",
                    " ")
                .Trim();

        while (remaining.Length >
               CharactersPerLine)
        {
            var breakAt =
                remaining.LastIndexOf(
                    ' ',
                    CharactersPerLine - 1,
                    CharactersPerLine);

            if (breakAt <= 0)
            {
                breakAt =
                    CharactersPerLine;
            }

            yield return remaining[..breakAt]
                .TrimEnd();

            remaining =
                remaining[breakAt..]
                    .TrimStart();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static string FormatMoney(
        PaymentReceiptResponse receipt,
        decimal amount)
    {
        var currency =
            GetCurrencyMarker(
                receipt);

        var amountText =
            amount.ToString(
                "#,##0.00",
                CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(
            currency)
                ? amountText
                : $"{currency} {amountText}";
    }

    private static string GetCurrencyMarker(
        PaymentReceiptResponse receipt)
    {
        if (IsPrintableAscii(
                receipt.CurrencySymbol))
        {
            return receipt.CurrencySymbol
                .Trim();
        }

        return ToAscii(
                receipt.CurrencyCode)
            .Trim();
    }

    private static bool IsPrintableAscii(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        return value.All(
            character =>
                character >= 32 &&
                character <= 126);
    }

    private static string GetPaymentTypeLabel(
        PaymentType paymentType)
    {
        return paymentType switch
        {
            PaymentType.Regular =>
                "Regular",

            PaymentType.Partial =>
                "Parcial",

            PaymentType.FullSettlement =>
                "Pago total",

            _ =>
                paymentType.ToString()
        };
    }

    private static void WriteLine(
        Stream stream,
        string value)
    {
        WriteText(
            stream,
            value);

        WriteCommand(
            stream,
            0x0D,
            0x0A);
    }

    private static void WriteText(
        Stream stream,
        string value)
    {
        var data =
            Encoding.ASCII.GetBytes(
                ToAscii(
                    value));

        stream.Write(
            data,
            0,
            data.Length);
    }

    private static void WriteCommand(
        Stream stream,
        params byte[] bytes)
    {
        stream.Write(
            bytes,
            0,
            bytes.Length);
    }

    private static string ToAscii(
        string? value)
    {
        if (string.IsNullOrEmpty(
                value))
        {
            return string.Empty;
        }

        var normalized =
            value.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder(
                normalized.Length);

        foreach (var character
                 in normalized)
        {
            var category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character);

            if (category ==
                UnicodeCategory
                    .NonSpacingMark)
            {
                continue;
            }

            if (character >= 32 &&
                character <= 126)
            {
                builder.Append(
                    character);
            }
        }

        return builder.ToString();
    }
}