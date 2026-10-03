using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Sanes.Application.Common.Files;

namespace Sanes.Infrastructure.Files;

public sealed class AzureBlobFileStorage
    : IFileStorage
{
    private readonly BlobContainerClient
        _containerClient;

    public AzureBlobFileStorage(
        BlobContainerClient containerClient)
    {
        _containerClient =
            containerClient
            ?? throw new ArgumentNullException(
                nameof(containerClient));
    }

    public async Task<StoredFileResult> SaveAsync(
        Guid tenantId,
        Guid loanGuaranteeId,
        string fileExtension,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be valid.",
                nameof(tenantId));
        }

        if (loanGuaranteeId == Guid.Empty)
        {
            throw new ArgumentException(
                "LoanGuaranteeId must be valid.",
                nameof(loanGuaranteeId));
        }

        ArgumentNullException.ThrowIfNull(
            content);

        if (!content.CanRead)
        {
            throw new InvalidOperationException(
                "The provided stream cannot be read.");
        }

        var normalizedExtension =
            NormalizeExtension(
                fileExtension);

        var fileName =
            $"{Guid.NewGuid():N}{normalizedExtension}";

        /*
         * Mantenemos exactamente el mismo formato lógico
         * utilizado por LocalFileStorage.
         *
         * La aplicación y PostgreSQL no necesitan conocer
         * si el archivo está en disco local o en Azure Blob.
         */
        var storageKey =
            string.Join(
                '/',
                "loan-guarantees",
                tenantId.ToString("N"),
                loanGuaranteeId.ToString("N"),
                fileName);

        var blobClient =
            _containerClient.GetBlobClient(
                storageKey);

        var uploaded =
            false;

        try
        {
            await blobClient.UploadAsync(
                content,
                overwrite: false,
                cancellationToken);

            uploaded =
                true;

            /*
             * Consultamos Blob Storage después del upload
             * para obtener el tamaño realmente almacenado.
             *
             * No confiamos únicamente en metadata recibida
             * desde la capa HTTP.
             */
            var properties =
                await blobClient.GetPropertiesAsync(
                    cancellationToken:
                        cancellationToken);

            return new StoredFileResult
            {
                StorageKey =
                    storageKey,

                FileSize =
                    properties.Value.ContentLength
            };
        }
        catch
        {
            /*
             * Si el blob llegó a crearse pero posteriormente
             * falla la obtención de propiedades, intentamos
             * eliminarlo para no dejar un binario huérfano.
             *
             * Nunca ocultamos la excepción original.
             */
            if (uploaded)
            {
                try
                {
                    await blobClient.DeleteIfExistsAsync(
                        DeleteSnapshotsOption
                            .IncludeSnapshots,
                        cancellationToken:
                            CancellationToken.None);
                }
                catch
                {
                    // Preserve original exception.
                }
            }

            throw;
        }
    }

    public async Task<StoredFileContent?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(
            storageKey);

        var blobClient =
            _containerClient.GetBlobClient(
                storageKey);

        var exists =
            await blobClient.ExistsAsync(
                cancellationToken);

        if (!exists.Value)
        {
            return null;
        }

        /*
         * DownloadStreamingAsync evita cargar el documento
         * completo en memoria antes de devolverlo a la API.
         */
        var download =
            await blobClient.DownloadStreamingAsync(
                cancellationToken:
                    cancellationToken);

        return new StoredFileContent
        {
            Content =
                download.Value.Content,

            FileSize =
                download.Value.Details.ContentLength
        };
    }

    public async Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(
            storageKey);

        var blobClient =
            _containerClient.GetBlobClient(
                storageKey);

        var result =
            await blobClient.ExistsAsync(
                cancellationToken);

        return result.Value;
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(
            storageKey);

        var blobClient =
            _containerClient.GetBlobClient(
                storageKey);

        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken:
                cancellationToken);
    }

    private static string NormalizeExtension(
        string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(
                fileExtension))
        {
            throw new ArgumentException(
                "File extension is required.",
                nameof(fileExtension));
        }

        var extension =
            fileExtension
                .Trim()
                .ToLowerInvariant();

        if (!extension.StartsWith('.'))
        {
            extension =
                "." + extension;
        }

        var extensionValue =
            extension[1..];

        if (
            extensionValue.Length == 0 ||
            extensionValue.Length > 10 ||
            extensionValue.Any(
                character =>
                    !char.IsLetterOrDigit(
                        character)))
        {
            throw new InvalidOperationException(
                "File extension is invalid.");
        }

        return extension;
    }

    private static void ValidateStorageKey(
        string storageKey)
    {
        if (string.IsNullOrWhiteSpace(
                storageKey))
        {
            throw new ArgumentException(
                "Storage key is required.",
                nameof(storageKey));
        }

        /*
         * Nuestros StorageKey son claves lógicas POSIX-style.
         * No aceptamos rutas absolutas, backslashes ni
         * segmentos de navegación.
         */
        if (
            storageKey.StartsWith("/", StringComparison.Ordinal) ||
            storageKey.Contains("\\", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Storage key is invalid.");
        }

        var segments =
            storageKey.Split(
                '/',
                StringSplitOptions
                    .RemoveEmptyEntries);

        if (
            segments.Length == 0 ||
            segments.Any(
                segment =>
                    segment is "." or ".."))
        {
            throw new InvalidOperationException(
                "Storage key is invalid.");
        }
    }
}
