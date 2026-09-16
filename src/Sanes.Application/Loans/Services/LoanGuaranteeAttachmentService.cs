using Sanes.Application.Common.Files;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.Services;

public class LoanGuaranteeAttachmentService
    : ILoanGuaranteeAttachmentService
{
    private const long MaximumFileSize =
        10L * 1024L * 1024L;

    private const int MaximumAttachments =
        10;

    private readonly ILoanRepository
        _loanRepository;

    private readonly ILoanGuaranteeAttachmentRepository
        _attachmentRepository;

    private readonly IFileStorage
        _fileStorage;

    public LoanGuaranteeAttachmentService(
        ILoanRepository loanRepository,
        ILoanGuaranteeAttachmentRepository attachmentRepository,
        IFileStorage fileStorage)
    {
        _loanRepository =
            loanRepository;

        _attachmentRepository =
            attachmentRepository;

        _fileStorage =
            fileStorage;
    }

    public async Task<List<LoanGuaranteeAttachmentResponse>?>
        GetAllAsync(
            Guid tenantId,
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        var guarantee =
            await GetGuaranteeAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (guarantee is null)
        {
            return null;
        }

        var attachments =
            await _attachmentRepository
                .GetByGuaranteeAsync(
                    tenantId,
                    guarantee.Id,
                    cancellationToken);

        return attachments
            .Select(Map)
            .ToList();
    }

    public async Task<LoanGuaranteeAttachmentResponse?>
        GetByIdAsync(
            Guid tenantId,
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        var guarantee =
            await GetGuaranteeAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (guarantee is null)
        {
            return null;
        }

        var attachment =
            await _attachmentRepository
                .GetByIdAsync(
                    tenantId,
                    guarantee.Id,
                    attachmentId,
                    cancellationToken);

        return attachment is null
            ? null
            : Map(attachment);
    }

    public async Task<LoanGuaranteeAttachmentContentResponse?>
        OpenContentAsync(
            Guid tenantId,
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        var guarantee =
            await GetGuaranteeAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (guarantee is null)
        {
            return null;
        }

        var attachment =
            await _attachmentRepository
                .GetByIdAsync(
                    tenantId,
                    guarantee.Id,
                    attachmentId,
                    cancellationToken);

        if (attachment is null)
        {
            return null;
        }

        var storedFile =
            await _fileStorage.OpenReadAsync(
                attachment.StorageKey,
                cancellationToken);

        if (storedFile is null)
        {
            throw new InvalidOperationException(
                "The attachment metadata exists, but the physical file could not be found.");
        }

        return new LoanGuaranteeAttachmentContentResponse
        {
            Content =
                storedFile.Content,

            FileName =
                attachment.OriginalFileName,

            ContentType =
                attachment.ContentType,

            FileSize =
                storedFile.FileSize
        };
    }

    public async Task<LoanGuaranteeAttachmentResponse?>
        UploadAsync(
            Guid tenantId,
            Guid appUserId,
            Guid loanId,
            string originalFileName,
            string contentType,
            long fileSize,
            Stream content,
            string? description,
            CancellationToken cancellationToken = default)
    {
        ValidateUploadMetadata(
            originalFileName,
            contentType,
            fileSize,
            description);

        var loan =
            await _loanRepository.GetByIdAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (loan is null ||
            loan.Guarantee is null)
        {
            return null;
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Attachments can only be uploaded while the loan is active.");
        }

        var activeCount =
            await _attachmentRepository
                .CountActiveByGuaranteeAsync(
                    tenantId,
                    loan.Guarantee.Id,
                    cancellationToken);

        if (activeCount >= MaximumAttachments)
        {
            throw new InvalidOperationException(
                $"A guarantee cannot have more than {MaximumAttachments} active attachments.");
        }

        var extension =
            ResolveAndValidateExtension(
                originalFileName,
                contentType);

        await ValidateFileSignatureAsync(
            content,
            contentType,
            cancellationToken);

        StoredFileResult? storedFile =
            null;

        try
        {
            storedFile =
                await _fileStorage.SaveAsync(
                    tenantId,
                    loan.Guarantee.Id,
                    extension,
                    content,
                    cancellationToken);

            /*
             * No confiamos exclusivamente en el tamaño
             * informado por la capa HTTP.
             */
            if (
                storedFile.FileSize <= 0 ||
                storedFile.FileSize > MaximumFileSize)
            {
                await _fileStorage.DeleteAsync(
                    storedFile.StorageKey,
                    CancellationToken.None);

                throw new InvalidOperationException(
                    $"File size must be greater than zero and cannot exceed {MaximumFileSize / 1024 / 1024} MB.");
            }

            var attachment =
                new LoanGuaranteeAttachment
                {
                    TenantId =
                        tenantId,

                    LoanGuaranteeId =
                        loan.Guarantee.Id,

                    UploadedByAppUserId =
                        appUserId,

                    OriginalFileName =
                        NormalizeOriginalFileName(
                            originalFileName),

                    StorageKey =
                        storedFile.StorageKey,

                    ContentType =
                        NormalizeContentType(
                            contentType),

                    FileSize =
                        storedFile.FileSize,

                    Description =
                        NormalizeOptional(
                            description),

                    CreatedAt =
                        DateTime.UtcNow,

                    IsDeleted =
                        false
                };

            await _attachmentRepository.AddAsync(
                attachment,
                cancellationToken);

            await _attachmentRepository.SaveChangesAsync(
                cancellationToken);

            return Map(
                attachment);
        }
        catch
        {
            /*
             * Si el archivo se escribió pero PostgreSQL falla,
             * eliminamos el archivo para no dejar binarios
             * huérfanos.
             */
            if (storedFile is not null)
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        storedFile.StorageKey,
                        CancellationToken.None);
                }
                catch
                {
                    /*
                     * No ocultamos la excepción original.
                     *
                     * Una política futura de mantenimiento
                     * podrá limpiar archivos huérfanos.
                     */
                }
            }

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        Guid tenantId,
        Guid appUserId,
        Guid loanId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var guarantee =
            await GetGuaranteeAsync(
                tenantId,
                loanId,
                cancellationToken);

        if (guarantee is null)
        {
            return false;
        }

        var attachment =
            await _attachmentRepository
                .GetByIdForUpdateAsync(
                    tenantId,
                    guarantee.Id,
                    attachmentId,
                    cancellationToken);

        if (attachment is null)
        {
            return false;
        }

        attachment.IsDeleted =
            true;

        attachment.DeletedAt =
            DateTime.UtcNow;

        attachment.DeletedByAppUserId =
            appUserId;

        await _attachmentRepository.SaveChangesAsync(
            cancellationToken);

        /*
         * Deliberadamente NO eliminamos el archivo físico.
         */
        return true;
    }

    private async Task<LoanGuarantee?>
        GetGuaranteeAsync(
            Guid tenantId,
            Guid loanId,
            CancellationToken cancellationToken)
    {
        var loan =
            await _loanRepository.GetByIdAsync(
                tenantId,
                loanId,
                cancellationToken);

        return loan?.Guarantee;
    }

    private static void ValidateUploadMetadata(
        string originalFileName,
        string contentType,
        long fileSize,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(
                originalFileName))
        {
            throw new InvalidOperationException(
                "File name is required.");
        }

        if (originalFileName.Length > 255)
        {
            throw new InvalidOperationException(
                "File name cannot exceed 255 characters.");
        }

        if (string.IsNullOrWhiteSpace(
                contentType))
        {
            throw new InvalidOperationException(
                "Content type is required.");
        }

        if (
            fileSize <= 0 ||
            fileSize > MaximumFileSize)
        {
            throw new InvalidOperationException(
                $"File size must be greater than zero and cannot exceed {MaximumFileSize / 1024 / 1024} MB.");
        }

        if (
            description is not null &&
            description.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "Attachment description cannot exceed 1000 characters.");
        }
    }

    private static string ResolveAndValidateExtension(
        string originalFileName,
        string contentType)
    {
        var extension =
            Path.GetExtension(
                originalFileName)
            .ToLowerInvariant();

        var normalizedContentType =
            NormalizeContentType(
                contentType);

        var isValid =
            normalizedContentType switch
            {
                "image/jpeg" =>
                    extension is ".jpg" or ".jpeg",

                "image/png" =>
                    extension == ".png",

                "application/pdf" =>
                    extension == ".pdf",

                _ =>
                    false
            };

        if (!isValid)
        {
            throw new InvalidOperationException(
                "Only JPEG, PNG, and PDF files with matching extensions are allowed.");
        }

        return extension;
    }

    private static async Task ValidateFileSignatureAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (content is null ||
            !content.CanRead)
        {
            throw new InvalidOperationException(
                "The uploaded file stream cannot be read.");
        }

        if (!content.CanSeek)
        {
            throw new InvalidOperationException(
                "The uploaded file stream must support seeking.");
        }

        var originalPosition =
            content.Position;

        try
        {
            var header =
                new byte[8];

            var bytesRead =
                await content.ReadAsync(
                    header.AsMemory(
                        0,
                        header.Length),
                    cancellationToken);

            var normalizedContentType =
                NormalizeContentType(
                    contentType);

            var valid =
                normalizedContentType switch
                {
                    "image/jpeg" =>
                        bytesRead >= 3 &&
                        header[0] == 0xFF &&
                        header[1] == 0xD8 &&
                        header[2] == 0xFF,

                    "image/png" =>
                        bytesRead >= 8 &&
                        header[0] == 0x89 &&
                        header[1] == 0x50 &&
                        header[2] == 0x4E &&
                        header[3] == 0x47 &&
                        header[4] == 0x0D &&
                        header[5] == 0x0A &&
                        header[6] == 0x1A &&
                        header[7] == 0x0A,

                    "application/pdf" =>
                        bytesRead >= 5 &&
                        header[0] == 0x25 &&
                        header[1] == 0x50 &&
                        header[2] == 0x44 &&
                        header[3] == 0x46 &&
                        header[4] == 0x2D,

                    _ =>
                        false
                };

            if (!valid)
            {
                throw new InvalidOperationException(
                    "The file content does not match the declared file type.");
            }
        }
        finally
        {
            /*
             * Es indispensable volver al inicio.
             *
             * De lo contrario SaveAsync almacenaría el archivo
             * sin los primeros bytes que acabamos de inspeccionar.
             */
            content.Position =
                originalPosition;
        }
    }

    private static string NormalizeOriginalFileName(
        string originalFileName)
    {
        /*
         * Aunque nunca utilizamos este valor como ruta física,
         * eliminamos cualquier componente de directorio.
         */
        var fileName =
            Path.GetFileName(
                originalFileName.Trim());

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            throw new InvalidOperationException(
                "File name is invalid.");
        }

        return fileName;
    }

    private static string NormalizeContentType(
        string contentType)
    {
        return contentType
            .Trim()
            .ToLowerInvariant();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
    }

    private static LoanGuaranteeAttachmentResponse Map(
        LoanGuaranteeAttachment attachment)
    {
        return new LoanGuaranteeAttachmentResponse
        {
            Id =
                attachment.Id,

            LoanGuaranteeId =
                attachment.LoanGuaranteeId,

            UploadedByAppUserId =
                attachment.UploadedByAppUserId,

            OriginalFileName =
                attachment.OriginalFileName,

            ContentType =
                attachment.ContentType,

            FileSize =
                attachment.FileSize,

            Description =
                attachment.Description,

            CreatedAt =
                attachment.CreatedAt
        };
    }
}