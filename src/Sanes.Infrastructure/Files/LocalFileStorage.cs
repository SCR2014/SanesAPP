using Sanes.Application.Common.Files;

namespace Sanes.Infrastructure.Files;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(
        string rootPath)
    {
        if (string.IsNullOrWhiteSpace(
                rootPath))
        {
            throw new ArgumentException(
                "File storage root path is required.",
                nameof(rootPath));
        }

        _rootPath =
            Path.GetFullPath(
                rootPath);

        Directory.CreateDirectory(
            _rootPath);
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

        if (content is null)
        {
            throw new ArgumentNullException(
                nameof(content));
        }

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
         * StorageKey siempre usa "/".
         *
         * De esta forma permanece portable aunque
         * el servidor cambie entre Windows y Linux.
         */
        var storageKey =
            string.Join(
                '/',
                "loan-guarantees",
                tenantId.ToString("N"),
                loanGuaranteeId.ToString("N"),
                fileName);

        var fullPath =
            ResolveStoragePath(
                storageKey);

        var directory =
            Path.GetDirectoryName(
                fullPath);

        if (string.IsNullOrWhiteSpace(
                directory))
        {
            throw new InvalidOperationException(
                "Could not determine the storage directory.");
        }

        Directory.CreateDirectory(
            directory);

        try
        {
            await using var output =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    options:
                        FileOptions.Asynchronous |
                        FileOptions.SequentialScan);

            await content.CopyToAsync(
                output,
                cancellationToken);

            await output.FlushAsync(
                cancellationToken);

            return new StoredFileResult
            {
                StorageKey =
                    storageKey,

                FileSize =
                    output.Length
            };
        }
        catch
        {
            /*
             * Si una escritura falla a mitad de camino,
             * no dejamos un archivo parcial.
             */
            if (File.Exists(
                    fullPath))
            {
                File.Delete(
                    fullPath);
            }

            throw;
        }
    }

    public Task<StoredFileContent?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        var fullPath =
            ResolveStoragePath(
                storageKey);

        if (!File.Exists(
                fullPath))
        {
            return Task.FromResult<
                StoredFileContent?>(
                    null);
        }

        var stream =
            new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options:
                    FileOptions.Asynchronous |
                    FileOptions.SequentialScan);

        StoredFileContent result =
            new()
            {
                Content =
                    stream,

                FileSize =
                    stream.Length
            };

        return Task.FromResult<
            StoredFileContent?>(
                result);
    }

    public Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        var fullPath =
            ResolveStoragePath(
                storageKey);

        return Task.FromResult(
            File.Exists(
                fullPath));
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        var fullPath =
            ResolveStoragePath(
                storageKey);

        if (File.Exists(
                fullPath))
        {
            File.Delete(
                fullPath);
        }

        return Task.CompletedTask;
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

        /*
         * No permitimos:
         *
         * .pdf.exe
         * ../pdf
         * .p/d/f
         * extensiones arbitrariamente largas
         */
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

    private string ResolveStoragePath(
        string storageKey)
    {
        if (string.IsNullOrWhiteSpace(
                storageKey))
        {
            throw new ArgumentException(
                "Storage key is required.",
                nameof(storageKey));
        }

        if (Path.IsPathRooted(
                storageKey))
        {
            throw new InvalidOperationException(
                "Absolute storage paths are not allowed.");
        }

        /*
         * StorageKey usa "/" independientemente
         * del sistema operativo.
         */
        var relativePath =
            storageKey.Replace(
                '/',
                Path.DirectorySeparatorChar);

        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootPath,
                    relativePath));

        /*
         * La ruta resultante debe permanecer
         * obligatoriamente dentro de _rootPath.
         *
         * Esto bloquea intentos como:
         *
         * ../../appsettings.json
         */
        var rootWithSeparator =
            _rootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!fullPath.StartsWith(
                rootWithSeparator,
                comparison))
        {
            throw new InvalidOperationException(
                "The storage key resolves outside the configured storage root.");
        }

        return fullPath;
    }
}