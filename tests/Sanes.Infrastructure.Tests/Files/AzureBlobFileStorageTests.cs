using System.Text;
using Azure.Storage.Blobs;
using Sanes.Infrastructure.Files;

namespace Sanes.Infrastructure.Tests.Files;

public class AzureBlobFileStorageTests
{
    private static AzureBlobFileStorage
        CreateValidationStorage()
    {
        var containerClient =
            new BlobContainerClient(
                new Uri(
                    "https://sanesapptest.invalid/test-container"));

        return new AzureBlobFileStorage(
            containerClient);
    }

    private static async Task<AzureBlobFileStorage>
        CreateAzuriteStorageAsync()
    {
        var containerName =
            $"sanes-tests-{Guid.NewGuid():N}";

        var containerClient =
            new BlobContainerClient(
                "UseDevelopmentStorage=true",
                containerName);

        await containerClient
            .CreateIfNotExistsAsync();

        return new AzureBlobFileStorage(
            containerClient);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        var storage =
            CreateValidationStorage();

        await using var content =
            new MemoryStream(
                [0x01]);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    storage.SaveAsync(
                        Guid.Empty,
                        Guid.NewGuid(),
                        ".pdf",
                        content));

        Assert.Contains(
            "TenantId",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyGuaranteeId_ThrowsArgumentException()
    {
        var storage =
            CreateValidationStorage();

        await using var content =
            new MemoryStream(
                [0x01]);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    storage.SaveAsync(
                        Guid.NewGuid(),
                        Guid.Empty,
                        ".pdf",
                        content));

        Assert.Contains(
            "LoanGuaranteeId",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithInvalidExtension_ThrowsBeforeNetworkCall()
    {
        var storage =
            CreateValidationStorage();

        await using var content =
            new MemoryStream(
                [0x01]);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    storage.SaveAsync(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "../pdf",
                        content));

        Assert.Contains(
            "extension",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/loan-guarantees/file.pdf")]
    [InlineData(@"loan-guarantees\file.pdf")]
    [InlineData("../file.pdf")]
    [InlineData("loan-guarantees/../file.pdf")]
    [InlineData("./file.pdf")]
    public async Task ExistsAsync_WithInvalidStorageKey_ThrowsBeforeNetworkCall(
        string storageKey)
    {
        var storage =
            CreateValidationStorage();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    storage.ExistsAsync(
                        storageKey));

        Assert.Contains(
            "Storage key",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyStorageKey_ThrowsArgumentException()
    {
        var storage =
            CreateValidationStorage();

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    storage.ExistsAsync(
                        string.Empty));

        Assert.Contains(
            "Storage key",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithAzurite_StoresFileAndReturnsExpectedMetadata()
    {
        var storage =
            await CreateAzuriteStorageAsync();

        var tenantId =
            Guid.NewGuid();

        var guaranteeId =
            Guid.NewGuid();

        var bytes =
            Encoding.UTF8.GetBytes(
                "SanesApp Azure Blob test");

        await using var content =
            new MemoryStream(
                bytes);

        var result =
            await storage.SaveAsync(
                tenantId,
                guaranteeId,
                ".pdf",
                content);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.StorageKey));

        Assert.Equal(
            bytes.LongLength,
            result.FileSize);

        Assert.StartsWith(
            $"loan-guarantees/{tenantId:N}/{guaranteeId:N}/",
            result.StorageKey,
            StringComparison.Ordinal);

        Assert.EndsWith(
            ".pdf",
            result.StorageKey,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenReadAsync_WithAzurite_ReturnsExactStoredBytes()
    {
        var storage =
            await CreateAzuriteStorageAsync();

        var bytes =
            Encoding.UTF8.GetBytes(
                "Exact blob content");

        await using var upload =
            new MemoryStream(
                bytes);

        var saved =
            await storage.SaveAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ".pdf",
                upload);

        var stored =
            await storage.OpenReadAsync(
                saved.StorageKey);

        Assert.NotNull(
            stored);

        Assert.Equal(
            bytes.LongLength,
            stored.FileSize);

        await using var downloaded =
            stored.Content;

        using var memory =
            new MemoryStream();

        await downloaded.CopyToAsync(
            memory);

        Assert.Equal(
            bytes,
            memory.ToArray());
    }

    [Fact]
    public async Task ExistsAsync_WithAzurite_ReturnsTrueForStoredBlob()
    {
        var storage =
            await CreateAzuriteStorageAsync();

        await using var content =
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    "Exists test"));

        var saved =
            await storage.SaveAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ".png",
                content);

        var exists =
            await storage.ExistsAsync(
                saved.StorageKey);

        Assert.True(
            exists);
    }

    [Fact]
    public async Task DeleteAsync_WithAzurite_RemovesBlob()
    {
        var storage =
            await CreateAzuriteStorageAsync();

        await using var content =
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    "Delete test"));

        var saved =
            await storage.SaveAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ".jpg",
                content);

        Assert.True(
            await storage.ExistsAsync(
                saved.StorageKey));

        await storage.DeleteAsync(
            saved.StorageKey);

        Assert.False(
            await storage.ExistsAsync(
                saved.StorageKey));
    }
}
