using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.LoanGuarantees;

public sealed class LoanGuaranteesWebService
    : ILoanGuaranteesWebService
{
    private readonly ISanesApiClient
        _apiClient;

    public LoanGuaranteesWebService(
        ISanesApiClient apiClient)
    {
        _apiClient =
            apiClient;
    }

    public async Task<LoanGuaranteeResponse?>
        GetGuaranteeAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}/guarantee");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await ReadJsonAsync<
            LoanGuaranteeResponse>(
                response,
                cancellationToken);
    }

    public async Task<LoanGuaranteeResponse?>
        CreateGuaranteeAsync(
            Guid loanId,
            CreateLoanGuaranteeRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/loans/{loanId:D}/guarantee")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                httpRequest,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await ReadJsonAsync<
            LoanGuaranteeResponse>(
                response,
                cancellationToken);
    }

    public async Task<LoanGuaranteeResponse?>
        UpdateGuaranteeAsync(
            Guid loanId,
            UpdateLoanGuaranteeRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/loans/{loanId:D}/guarantee")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                httpRequest,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await ReadJsonAsync<
            LoanGuaranteeResponse>(
                response,
                cancellationToken);
    }

    public async Task<
        List<LoanGuaranteeAttachmentResponse>?>
        GetAttachmentsAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}/guarantee/attachments");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        /*
         * El backend devuelve 404 tanto cuando:
         *
         * - el préstamo no pertenece al Tenant, como
         * - el préstamo no tiene garantía.
         */
        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await ReadJsonAsync<
            List<LoanGuaranteeAttachmentResponse>>(
                response,
                cancellationToken);
    }

    public async Task<
        LoanGuaranteeAttachmentResponse?>
        UploadAttachmentAsync(
            Guid loanId,
            Stream content,
            string fileName,
            string contentType,
            long fileSize,
            string? description,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ArgumentNullException.ThrowIfNull(
            content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "El archivo seleccionado no puede leerse.",
                nameof(content));
        }

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.",
                nameof(fileName));
        }

        if (fileName.Length > 255)
        {
            throw new ArgumentException(
                "El nombre del archivo no puede superar 255 caracteres.",
                nameof(fileName));
        }

        if (
            fileSize <= 0 ||
            fileSize >
                10L * 1024L * 1024L)
        {
            throw new ArgumentException(
                "El archivo debe tener contenido y no puede superar 10 MB.",
                nameof(fileSize));
        }

        if (
            description is not null &&
            description.Trim().Length > 1000)
        {
            throw new ArgumentException(
                "La descripción del documento no puede superar 1000 caracteres.",
                nameof(description));
        }

        var normalizedContentType =
            ResolveContentType(
                fileName,
                contentType);

        using var form =
            new MultipartFormDataContent();

        using var fileContent =
            new StreamContent(
                content);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                normalizedContentType);

        fileContent.Headers.ContentLength =
            fileSize;

        form.Add(
            fileContent,
            "file",
            Path.GetFileName(
                fileName));

        var normalizedDescription =
            string.IsNullOrWhiteSpace(
                    description)
                ? null
                : description.Trim();

        if (normalizedDescription is not null)
        {
            form.Add(
                new StringContent(
                    normalizedDescription),
                "description");
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/loans/{loanId:D}/guarantee/attachments")
            {
                Content =
                    form
            };

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return await ReadJsonAsync<
            LoanGuaranteeAttachmentResponse>(
                response,
                cancellationToken);
    }

    public async Task<
        LoanGuaranteeDocumentDownload?>
        DownloadAttachmentAsync(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ValidateAttachmentId(
            attachmentId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}/guarantee/attachments/{attachmentId:D}/content");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        var bytes =
            await response.Content
                .ReadAsByteArrayAsync(
                    cancellationToken);

        var contentType =
            response.Content.Headers
                .ContentType?
                .MediaType;

        if (string.IsNullOrWhiteSpace(
            contentType))
        {
            contentType =
                "application/octet-stream";
        }

        var fileName =
            response.Content.Headers
                .ContentDisposition?
                .FileNameStar
            ??
            response.Content.Headers
                .ContentDisposition?
                .FileName;

        fileName =
            string.IsNullOrWhiteSpace(
                    fileName)
                ? $"documento-{attachmentId:D}"
                : fileName.Trim(
                    '"');

        return new LoanGuaranteeDocumentDownload
        {
            Content =
                bytes,

            FileName =
                fileName,

            ContentType =
                contentType
        };
    }

    public async Task<bool>
        DeleteAttachmentAsync(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ValidateAttachmentId(
            attachmentId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/loans/{loanId:D}/guarantee/attachments/{attachmentId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        return true;
    }

    private static void ValidateLoanId(
        Guid loanId)
    {
        if (loanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del préstamo no es válido.",
                nameof(loanId));
        }
    }

    private static void ValidateAttachmentId(
        Guid attachmentId)
    {
        if (attachmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del documento no es válido.",
                nameof(attachmentId));
        }
    }

    private static string ResolveContentType(
        string fileName,
        string? contentType)
    {
        var extension =
            Path.GetExtension(
                    fileName)
                .ToLowerInvariant();

        var normalized =
            contentType?
                .Trim()
                .ToLowerInvariant();

        /*
         * Algunos navegadores pueden entregar
         * application/octet-stream.
         *
         * Como el backend igualmente valida extensión,
         * MIME y firma real, podemos resolver los tres
         * tipos soportados a partir de la extensión.
         */
        if (
            string.IsNullOrWhiteSpace(
                normalized) ||
            normalized ==
                "application/octet-stream")
        {
            normalized =
                extension switch
                {
                    ".jpg" or ".jpeg" =>
                        "image/jpeg",

                    ".png" =>
                        "image/png",

                    ".pdf" =>
                        "application/pdf",

                    _ =>
                        normalized
                };
        }

        return normalized switch
        {
            "image/jpeg"
                when extension is
                    ".jpg" or ".jpeg" =>
                normalized,

            "image/png"
                when extension == ".png" =>
                normalized,

            "application/pdf"
                when extension == ".pdf" =>
                normalized,

            _ =>
                throw new ArgumentException(
                    "Solo se permiten archivos JPEG, PNG o PDF.")
        };
    }

    private static async Task<T>
        ReadJsonAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        var value =
            await response.Content
                .ReadFromJsonAsync<T>(
                    cancellationToken:
                        cancellationToken);

        if (value is null)
        {
            throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía.");
        }

        return value;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode ==
            HttpStatusCode.RequestEntityTooLarge)
        {
            throw new ArgumentException(
                "El archivo supera el tamaño máximo permitido.");
        }

        var apiMessage =
            await ReadApiMessageAsync(
                response,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                TranslateApiMessage(
                    apiMessage));
        }

        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(
                    apiMessage)
                ? $"Sanes.Api respondió con {(int)response.StatusCode}."
                : apiMessage,
            inner: null,
            statusCode:
                response.StatusCode);
    }

    private static async Task<string?>
        ReadApiMessageAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        var text =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
            text))
        {
            return null;
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    text);

            if (
                document.RootElement.ValueKind ==
                    JsonValueKind.Object &&
                document.RootElement
                    .TryGetProperty(
                        "message",
                        out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Respuesta no JSON.
        }

        return text;
    }

    private static string TranslateApiMessage(
        string? message)
    {
        if (string.IsNullOrWhiteSpace(
            message))
        {
            return "La operación no pudo completarse.";
        }

        var normalized =
            message.Trim();

        if (normalized.Equals(
                "Guarantee type is invalid.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El tipo de garantía no es válido.";
        }

        if (normalized.Equals(
                "Guarantee reference is required.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La referencia de la garantía es obligatoria.";
        }

        if (normalized.Equals(
                "Guarantee reference cannot exceed 150 characters.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La referencia de la garantía no puede superar 150 caracteres.";
        }

        if (normalized.Equals(
                "Guarantee description cannot exceed 1000 characters.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La descripción de la garantía no puede superar 1000 caracteres.";
        }

        if (normalized.Equals(
                "Guarantees can only be added to active loans.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Solo se pueden agregar garantías a préstamos activos.";
        }

        if (normalized.Equals(
                "Guarantees can only be updated for active loans.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Solo se pueden modificar garantías de préstamos activos.";
        }

        if (normalized.Equals(
                "The loan already has a guarantee.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El préstamo ya tiene una garantía registrada.";
        }

        if (normalized.Equals(
                "The loan does not have a guarantee.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El préstamo no tiene una garantía registrada.";
        }

        if (normalized.Equals(
                "Attachments can only be uploaded while the loan is active.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Solo se pueden subir documentos mientras el préstamo esté activo.";
        }

        if (normalized.Contains(
                "cannot have more than 10 active attachments",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La garantía no puede tener más de 10 documentos activos.";
        }

        if (normalized.Contains(
                "cannot exceed 10 MB",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El archivo debe tener contenido y no puede superar 10 MB.";
        }

        if (normalized.Equals(
                "Only JPEG, PNG, and PDF files with matching extensions are allowed.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Solo se permiten archivos JPEG, PNG o PDF con una extensión correcta.";
        }

        if (normalized.Equals(
                "The file content does not match the declared file type.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El contenido del archivo no coincide con el tipo de archivo declarado.";
        }

        if (normalized.Equals(
                "Attachment description cannot exceed 1000 characters.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La descripción del documento no puede superar 1000 caracteres.";
        }

        if (normalized.Equals(
                "File name cannot exceed 255 characters.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El nombre del archivo no puede superar 255 caracteres.";
        }

        if (normalized.Equals(
                "The attachment metadata exists, but the physical file could not be found.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El documento está registrado, pero el archivo físico no pudo encontrarse.";
        }

        if (normalized.Equals(
                "LoanId must be a valid identifier.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El identificador del préstamo no es válido.";
        }

        if (normalized.Equals(
                "LoanId and AttachmentId must be valid identifiers.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "El identificador del préstamo o del documento no es válido.";
        }

        if (normalized.Equals(
                "A file is required.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Debes seleccionar un archivo.";
        }

        return normalized;
    }
}