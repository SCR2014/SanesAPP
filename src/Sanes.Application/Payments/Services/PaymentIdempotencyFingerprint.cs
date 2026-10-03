using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Payments.DTOs;

namespace Sanes.Application.Payments.Services;

public static class PaymentIdempotencyFingerprint
{
    private const string FingerprintVersion =
        "v1";

    private const string AdministrativeOperation =
        "administrative-payment";

    private const string FieldCollectionOperation =
        "field-collection-payment";

    public static string CreateAdministrative(
        CreatePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        /*
         * PaymentDate sí forma parte de un pago administrativo
         * porque viene definido explícitamente por el caller.
         *
         * Se normaliza exactamente con la misma semántica que
         * PaymentService antes de calcular el fingerprint.
         */
        var paymentDate =
            NormalizeUtc(
                request.PaymentDate);

        return ComputeHash(
            new[]
            {
                Field(
                    "version",
                    FingerprintVersion),

                Field(
                    "operation",
                    AdministrativeOperation),

                Field(
                    "loanId",
                    FormatGuid(
                        request.LoanId)),

                Field(
                    "amount",
                    FormatDecimal(
                        request.Amount)),

                Field(
                    "paymentDate",
                    paymentDate.ToString(
                        "O",
                        CultureInfo.InvariantCulture)),

                Field(
                    "paymentType",
                    Convert.ToInt32(
                            request.PaymentType,
                            CultureInfo.InvariantCulture)
                        .ToString(
                            CultureInfo.InvariantCulture)),

                Field(
                    "collectedByAppUserId",
                    FormatNullableGuid(
                        request.CollectedByAppUserId)),

                Field(
                    "collectionRouteId",
                    FormatNullableGuid(
                        request.CollectionRouteId)),

                Field(
                    "notes",
                    NormalizeOptional(
                        request.Notes))
            });
    }

    public static string CreateFieldCollection(
        Guid appUserId,
        CreateFieldCollectionPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        /*
         * PaymentDate NO participa aquí.
         *
         * FieldCollectionService genera DateTime.UtcNow para
         * cada intento. Incluir esa fecha haría que un retry
         * legítimo produjera un fingerprint diferente.
         *
         * AppUserId sí forma parte de la operación porque viene
         * del usuario autenticado y determina quién realizó el
         * cobro.
         */
        return ComputeHash(
            new[]
            {
                Field(
                    "version",
                    FingerprintVersion),

                Field(
                    "operation",
                    FieldCollectionOperation),

                Field(
                    "appUserId",
                    FormatGuid(
                        appUserId)),

                Field(
                    "collectionRouteId",
                    FormatGuid(
                        request.CollectionRouteId)),

                Field(
                    "loanId",
                    FormatGuid(
                        request.LoanId)),

                Field(
                    "amount",
                    FormatDecimal(
                        request.Amount)),

                Field(
                    "paymentType",
                    Convert.ToInt32(
                            request.PaymentType,
                            CultureInfo.InvariantCulture)
                        .ToString(
                            CultureInfo.InvariantCulture)),

                Field(
                    "notes",
                    NormalizeOptional(
                        request.Notes))
            });
    }

    private static string ComputeHash(
        IEnumerable<string> fields)
    {
        /*
         * Los campos ya vienen en un orden fijo.
         *
         * Cada valor usa length-prefix encoding para evitar
         * ambigüedades aunque Notes contenga saltos de línea,
         * separadores, "=" u otros caracteres.
         */
        var canonicalValue =
            string.Concat(
                fields);

        var bytes =
            Encoding.UTF8.GetBytes(
                canonicalValue);

        var hash =
            SHA256.HashData(
                bytes);

        /*
         * SHA-256 => 32 bytes => 64 caracteres hexadecimales.
         *
         * Coincide con RequestHash varchar(64).
         */
        return Convert.ToHexString(
            hash);
    }

    private static string Field(
        string name,
        string? value)
    {
        /*
         * Null y string vacío deben seguir siendo estados
         * distinguibles.
         */
        var normalizedValue =
            value is null
                ? "<NULL>"
                : value;

        return string.Concat(
            name.Length.ToString(
                CultureInfo.InvariantCulture),
            ":",
            name,
            normalizedValue.Length.ToString(
                CultureInfo.InvariantCulture),
            ":",
            normalizedValue);
    }

    private static string FormatGuid(
        Guid value)
    {
        return value.ToString(
            "D");
    }

    private static string? FormatNullableGuid(
    Guid? value)
    {
        return value.HasValue
            ? FormatGuid(
                value.Value)
            : null;
    }

    private static string FormatDecimal(
        decimal value)
    {
        /*
         * G29 elimina ceros decimales no significativos.
         *
         * 100
         * 100.0
         * 100.00
         *
         * representan el mismo monto financiero.
         */
        return value.ToString(
            "G29",
            CultureInfo.InvariantCulture);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value.Trim();
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        if (value.Kind ==
            DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind ==
            DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }
}