using System.Text;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.Printing;

namespace Sanes.Web.Tests.Printing;

public class EscPosReceiptEncoderTests
{
    private readonly EscPosReceiptEncoder _encoder =
        new();

    [Fact]
    public void Encode_ProducesReceiptSnapshotContent()
    {
        var receipt =
            CreateReceipt();

        var bytes =
            _encoder.Encode(
                receipt);

        var text =
            NormalizeWhitespace(
                Decode(
                    bytes));

        Assert.Contains(
            "Sanes Web Test",
            text);

        Assert.Contains(
            "Sanes Web Test SRL",
            text);

        Assert.Contains(
            "RECIBO DE PAGO",
            text);

        Assert.Contains(
            "REC-2026-000002",
            text);

        Assert.Contains(
            "Jose Bolivar Osoria Torres",
            text);

        Assert.Contains(
            "Tipo: Parcial",
            text);

        Assert.Contains(
            "RD$ 20.00",
            text);

        Assert.Contains(
            "RD$ 580.00",
            text);

        Assert.Contains(
            "Cobrador: Cobrador Web Test",
            text);

        Assert.Contains(
            "Prueba de pago parcial Web",
            text);
    }

    [Fact]
    public void Encode_UsesPersistedFinancialValues()
    {
        var receipt =
            CreateReceipt();

        receipt.AmountReceived =
            125.50m;

        receipt.LateFeeAmountApplied =
            10.25m;

        receipt.LoanBalanceAmountApplied =
            115.25m;

        receipt.ContractualBalanceAfter =
            800.75m;

        receipt.LateFeeBalanceAfter =
            15.50m;

        receipt.TotalOutstandingAfter =
            816.25m;

        var text =
            NormalizeWhitespace(
                Decode(
                    _encoder.Encode(
                        receipt)));

        Assert.Contains(
            "RD$ 125.50",
            text);

        Assert.Contains(
            "RD$ 10.25",
            text);

        Assert.Contains(
            "RD$ 115.25",
            text);

        Assert.Contains(
            "RD$ 800.75",
            text);

        Assert.Contains(
            "RD$ 15.50",
            text);

        Assert.Contains(
            "RD$ 816.25",
            text);
    }

    [Theory]
    [InlineData(
        PaymentType.Regular,
        "Tipo: Regular")]
    [InlineData(
        PaymentType.Partial,
        "Tipo: Parcial")]
    [InlineData(
        PaymentType.FullSettlement,
        "Tipo: Pago total")]
    public void Encode_MapsPaymentType(
        PaymentType paymentType,
        string expected)
    {
        var receipt =
            CreateReceipt();

        receipt.PaymentType =
            paymentType;

        var text =
            Decode(
                _encoder.Encode(
                    receipt));

        Assert.Contains(
            expected,
            text);
    }

    [Fact]
    public void Encode_NormalizesAccentedCharacters()
    {
        var receipt =
            CreateReceipt();

        receipt.ClientName =
            "José Núñez";

        receipt.Notes =
            "Préstamo pagado según acuerdo";

        var text =
            Decode(
                _encoder.Encode(
                    receipt));

        Assert.Contains(
            "Jose Nunez",
            text);

        Assert.Contains(
            "Prestamo",
            text);

        Assert.Contains(
            "pagado",
            text);

        Assert.Contains(
            "segun",
            text);

        Assert.Contains(
            "acuerdo",
            text);

        Assert.DoesNotContain(
            "José",
            text);

        Assert.DoesNotContain(
            "Núñez",
            text);

        Assert.DoesNotContain(
            "Préstamo",
            text);

        Assert.DoesNotContain(
            "según",
            text);
    }

    [Fact]
    public void Encode_FallsBackToCurrencyCode_WhenSymbolIsNotAscii()
    {
        var receipt =
            CreateReceipt();

        receipt.CurrencySymbol =
            "€";

        receipt.CurrencyCode =
            "EUR";

        var text =
            Decode(
                _encoder.Encode(
                    receipt));

        Assert.Contains(
            "EUR 20.00",
            text);

        Assert.DoesNotContain(
            "? 20.00",
            text);
    }

    [Fact]
    public void Encode_IncludesEscPosInitializationAndFeedCommands()
    {
        var receipt =
            CreateReceipt();

        var bytes =
            _encoder.Encode(
                receipt);

        Assert.True(
            bytes.Length > 5);

        Assert.Equal(
            0x1B,
            bytes[0]);

        Assert.Equal(
            0x40,
            bytes[1]);

        Assert.Equal(
            0x1B,
            bytes[^3]);

        Assert.Equal(
            0x64,
            bytes[^2]);

        Assert.Equal(
            0x04,
            bytes[^1]);
    }

    [Fact]
    public void Encode_WrapsLongTextToThermalWidth()
    {
        var receipt =
            CreateReceipt();

        receipt.ClientName =
            "Cliente Con Un Nombre Extremadamente Largo Para Probar El Ajuste";

        var text =
            Decode(
                _encoder.Encode(
                    receipt));

        var lines =
            text
                .Replace(
                    "\r",
                    string.Empty)
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

        Assert.All(
            lines,
            line =>
            {
                Assert.True(
                    line.Length <= 32,
                    $"La línea excede 32 caracteres: '{line}'");
            });
    }

    private static string Decode(
        byte[] bytes)
    {
        using var output =
            new MemoryStream();

        for (var index = 0;
            index < bytes.Length;
            index++)
        {
            var current =
                bytes[index];

            if (current == 0x1B &&
                index + 1 < bytes.Length)
            {
                var command =
                    bytes[index + 1];

                // ESC @
                if (command == 0x40)
                {
                    index += 1;
                    continue;
                }

                // ESC a n
                // ESC E n
                // ESC d n
                if ((command == 0x61 ||
                    command == 0x45 ||
                    command == 0x64) &&
                    index + 2 < bytes.Length)
                {
                    index += 2;
                    continue;
                }
            }

            output.WriteByte(
                current);
        }

        return Encoding.ASCII
            .GetString(
                output.ToArray());
    }
    private static string NormalizeWhitespace(
        string value)
    {
        return string.Join(
            " ",
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
    }

    private static PaymentReceiptResponse
        CreateReceipt()
    {
        return new PaymentReceiptResponse
        {
            Id =
                Guid.NewGuid(),

            PaymentId =
                Guid.NewGuid(),

            ReceiptNumber =
                "REC-2026-000002",

            ReceiptYear =
                2026,

            SequenceNumber =
                2,

            TenantName =
                "Sanes Web Test",

            TenantLegalName =
                "Sanes Web Test SRL",

            CurrencyCode =
                "DOP",

            CurrencySymbol =
                "RD$",

            ClientId =
                Guid.NewGuid(),

            ClientName =
                "Jose Bolivar Osoria Torres",

            LoanId =
                Guid.NewGuid(),

            PaymentDate =
                new DateTime(
                    2026,
                    9,
                    27,
                    14,
                    55,
                    0,
                    DateTimeKind.Utc),

            PaymentType =
                PaymentType.Partial,

            AmountReceived =
                20.00m,

            LateFeeAmountApplied =
                0.00m,

            LoanBalanceAmountApplied =
                20.00m,

            ContractualBalanceAfter =
                580.00m,

            LateFeeBalanceAfter =
                0.00m,

            TotalOutstandingAfter =
                580.00m,

            CollectedByAppUserId =
                Guid.NewGuid(),

            CollectedByName =
                "Cobrador Web Test",

            Notes =
                "Prueba de pago parcial Web",

            CreatedAt =
                new DateTime(
                    2026,
                    9,
                    27,
                    14,
                    55,
                    0,
                    DateTimeKind.Utc)
        };
    }
}