namespace Sanes.Application.Common.Files;

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        Guid tenantId,
        Guid loanGuaranteeId,
        string fileExtension,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<StoredFileContent?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}