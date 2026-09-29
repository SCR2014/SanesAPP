using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.LoanGuarantees;

namespace Sanes.Web.Tests;

public class LoanGuaranteesWebServiceTests
{
    [Fact]
    public async Task GetGuaranteeAsync_SendsExpectedRequestAndReturnsGuarantee()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var guarantee =
            CreateGuaranteeResponse();

        apiClient.EnqueueResponse(
            JsonResponse(
                HttpStatusCode.OK,
                guarantee));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetGuaranteeAsync(
                loanId);

        Assert.NotNull(
            result);

        Assert.Equal(
            guarantee.Id,
            result.Id);

        Assert.Equal(
            guarantee.Type,
            result.Type);

        Assert.Equal(
            guarantee.Reference,
            result.Reference);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee",
            recorded.Uri);

        Assert.Null(
            recorded.Body);

        Assert.False(
            recorded.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Uri.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetGuaranteeAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetGuaranteeAsync(
                Guid.NewGuid());

        Assert.Null(
            result);

        Assert.Single(
            apiClient.Requests);
    }

    [Fact]
    public async Task GetGuaranteeAsync_WithEmptyLoanId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service
                            .GetGuaranteeAsync(
                                Guid.Empty));

        Assert.Contains(
            "préstamo",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task CreateGuaranteeAsync_SendsPostWithExpectedBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var response =
            CreateGuaranteeResponse(
                LoanGuaranteeType.Collateral,
                "VEH-001",
                "Vehículo en garantía");

        apiClient.EnqueueResponse(
            JsonResponse(
                HttpStatusCode.Created,
                response));

        var service =
            CreateService(
                apiClient);

        var request =
            new CreateLoanGuaranteeRequest
            {
                Type =
                    LoanGuaranteeType.Collateral,

                Reference =
                    "VEH-001",

                Description =
                    "Vehículo en garantía"
            };

        var result =
            await service.CreateGuaranteeAsync(
                loanId,
                request);

        Assert.NotNull(
            result);

        Assert.Equal(
            "VEH-001",
            result.Reference);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee",
            recorded.Uri);

        Assert.NotNull(
            recorded.Body);

        var sent =
            DeserializeBody<
                CreateLoanGuaranteeRequest>(
                    recorded.Body);

        Assert.Equal(
            LoanGuaranteeType.Collateral,
            sent.Type);

        Assert.Equal(
            "VEH-001",
            sent.Reference);

        Assert.Equal(
            "Vehículo en garantía",
            sent.Description);

        Assert.False(
            recorded.Body.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Body.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateGuaranteeAsync_SendsPutWithExpectedBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                HttpStatusCode.OK,
                CreateGuaranteeResponse(
                    LoanGuaranteeType.Guarantor,
                    "GAR-002",
                    "Garante actualizado")));

        var service =
            CreateService(
                apiClient);

        var request =
            new UpdateLoanGuaranteeRequest
            {
                Type =
                    LoanGuaranteeType.Guarantor,

                Reference =
                    "GAR-002",

                Description =
                    "Garante actualizado"
            };

        var result =
            await service.UpdateGuaranteeAsync(
                loanId,
                request);

        Assert.NotNull(
            result);

        Assert.Equal(
            LoanGuaranteeType.Guarantor,
            result.Type);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Put,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee",
            recorded.Uri);

        Assert.NotNull(
            recorded.Body);

        var sent =
            DeserializeBody<
                UpdateLoanGuaranteeRequest>(
                    recorded.Body);

        Assert.Equal(
            LoanGuaranteeType.Guarantor,
            sent.Type);

        Assert.Equal(
            "GAR-002",
            sent.Reference);

        Assert.False(
            recorded.Body.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAttachmentsAsync_SendsExpectedRequestAndReturnsAttachments()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var attachment =
            CreateAttachmentResponse();

        apiClient.EnqueueResponse(
            JsonResponse(
                HttpStatusCode.OK,
                new List<
                    LoanGuaranteeAttachmentResponse>
                {
                    attachment
                }));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetAttachmentsAsync(
                loanId);

        Assert.NotNull(
            result);

        var returned =
            Assert.Single(
                result);

        Assert.Equal(
            attachment.Id,
            returned.Id);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee/attachments",
            recorded.Uri);

        Assert.False(
            recorded.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAttachmentsAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetAttachmentsAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task UploadAttachmentAsync_SendsMultipartAndResolvesContentType()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var attachment =
            CreateAttachmentResponse(
                fileName:
                    "garantia.pdf",
                contentType:
                    "application/pdf",
                description:
                    "Contrato firmado");

        apiClient.EnqueueResponse(
            JsonResponse(
                HttpStatusCode.Created,
                attachment));

        var service =
            CreateService(
                apiClient);

        var bytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.4\n%%EOF");

        await using var stream =
            new MemoryStream(
                bytes);

        var result =
            await service.UploadAttachmentAsync(
                loanId,
                stream,
                "garantia.pdf",
                "application/octet-stream",
                bytes.LongLength,
                "  Contrato firmado  ");

        Assert.NotNull(
            result);

        Assert.Equal(
            "garantia.pdf",
            result.OriginalFileName);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee/attachments",
            recorded.Uri);

        Assert.NotNull(
            recorded.Body);

        Assert.Contains(
            "garantia.pdf",
            recorded.Body,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "application/pdf",
            recorded.Body,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Contrato firmado",
            recorded.Body,
            StringComparison.Ordinal);

        Assert.Contains(
            "%PDF-1.4",
            recorded.Body,
            StringComparison.Ordinal);

        Assert.False(
            recorded.Body.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Body.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadAttachmentAsync_WithUnsupportedExtension_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var bytes =
            Encoding.UTF8.GetBytes(
                "test");

        await using var stream =
            new MemoryStream(
                bytes);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service
                            .UploadAttachmentAsync(
                                Guid.NewGuid(),
                                stream,
                                "archivo.exe",
                                "application/octet-stream",
                                bytes.LongLength,
                                null));

        Assert.Contains(
            "JPEG",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task UploadAttachmentAsync_WithFileLargerThan10Mb_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var bytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.4");

        await using var stream =
            new MemoryStream(
                bytes);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service
                            .UploadAttachmentAsync(
                                Guid.NewGuid(),
                                stream,
                                "garantia.pdf",
                                "application/pdf",
                                (10L * 1024L * 1024L) + 1L,
                                null));

        Assert.Contains(
            "10 MB",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task UploadAttachmentAsync_WhenApiRejectsEleventhAttachment_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "A guarantee cannot have more than 10 active attachments."));

        var service =
            CreateService(
                apiClient);

        var bytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.4\n%%EOF");

        await using var stream =
            new MemoryStream(
                bytes);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service
                            .UploadAttachmentAsync(
                                Guid.NewGuid(),
                                stream,
                                "garantia.pdf",
                                "application/pdf",
                                bytes.LongLength,
                                null));

        Assert.Equal(
            "La garantía no puede tener más de 10 documentos activos.",
            exception.Message);

        Assert.Single(
            apiClient.Requests);
    }

    [Fact]
    public async Task DownloadAttachmentAsync_ReturnsBytesFileNameAndContentType()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var attachmentId =
            Guid.NewGuid();

        var bytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.4\nDocumento\n%%EOF");

        var response =
            new HttpResponseMessage(
                HttpStatusCode.OK);

        response.Content =
            new ByteArrayContent(
                bytes);

        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/pdf");

        response.Content.Headers.ContentDisposition =
            new ContentDispositionHeaderValue(
                "attachment")
            {
                FileName =
                    "\"contrato.pdf\""
            };

        apiClient.EnqueueResponse(
            response);

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DownloadAttachmentAsync(
                loanId,
                attachmentId);

        Assert.NotNull(
            result);

        Assert.Equal(
            bytes,
            result.Content);

        Assert.Equal(
            "contrato.pdf",
            result.FileName);

        Assert.Equal(
            "application/pdf",
            result.ContentType);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee/attachments/{attachmentId:D}/content",
            recorded.Uri);

        Assert.Null(
            recorded.Body);
    }

    [Fact]
    public async Task DownloadAttachmentAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DownloadAttachmentAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task DownloadAttachmentAsync_WithEmptyAttachmentId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service
                            .DownloadAttachmentAsync(
                                Guid.NewGuid(),
                                Guid.Empty));

        Assert.Contains(
            "documento",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_SendsDeleteAndReturnsTrue()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var attachmentId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DeleteAttachmentAsync(
                loanId,
                attachmentId);

        Assert.True(
            result);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Delete,
            recorded.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/guarantee/attachments/{attachmentId:D}",
            recorded.Uri);

        Assert.Null(
            recorded.Body);

        Assert.False(
            recorded.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Uri.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DeleteAttachmentAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

        Assert.False(
            result);
    }

    [Fact]
    public void GuaranteeFormModel_FromResponse_MapsExistingGuarantee()
    {
        var response =
            CreateGuaranteeResponse(
                LoanGuaranteeType.Collateral,
                "VEH-999",
                "Vehículo");

        var model =
            LoanGuaranteeFormModel
                .FromResponse(
                    response);

        Assert.Equal(
            LoanGuaranteeType.Collateral,
            model.Type);

        Assert.Equal(
            "VEH-999",
            model.Reference);

        Assert.Equal(
            "Vehículo",
            model.Description);
    }

    [Fact]
    public void GuaranteeFormModel_ToRequests_TrimsAndMapsValues()
    {
        var model =
            new LoanGuaranteeFormModel
            {
                Type =
                    LoanGuaranteeType.Guarantor,

                Reference =
                    "  GAR-123  ",

                Description =
                    "  Garante principal  "
            };

        var createRequest =
            model.ToCreateRequest();

        var updateRequest =
            model.ToUpdateRequest();

        Assert.Equal(
            LoanGuaranteeType.Guarantor,
            createRequest.Type);

        Assert.Equal(
            "GAR-123",
            createRequest.Reference);

        Assert.Equal(
            "Garante principal",
            createRequest.Description);

        Assert.Equal(
            LoanGuaranteeType.Guarantor,
            updateRequest.Type);

        Assert.Equal(
            "GAR-123",
            updateRequest.Reference);

        Assert.Equal(
            "Garante principal",
            updateRequest.Description);
    }

    [Fact]
    public void UploadFormModel_NormalizesOptionalDescription()
    {
        var model =
            new LoanGuaranteeUploadFormModel
            {
                Description =
                    "  Matrícula del vehículo  "
            };

        Assert.Equal(
            "Matrícula del vehículo",
            model.GetNormalizedDescription());

        model.Description =
            "   ";

        Assert.Null(
            model.GetNormalizedDescription());
    }

    private static LoanGuaranteesWebService
        CreateService(
            TestSanesApiClient apiClient)
    {
        return new LoanGuaranteesWebService(
            apiClient);
    }

    private static LoanGuaranteeResponse
        CreateGuaranteeResponse(
            LoanGuaranteeType type =
                LoanGuaranteeType
                    .IdentificationDocument,
            string reference =
                "ID-001",
            string? description =
                "Documento de garantía")
    {
        var now =
            DateTime.UtcNow;

        return new LoanGuaranteeResponse
        {
            Id =
                Guid.NewGuid(),

            Type =
                type,

            Reference =
                reference,

            Description =
                description,

            CreatedAt =
                now.AddMinutes(-5),

            UpdatedAt =
                now
        };
    }

    private static LoanGuaranteeAttachmentResponse
        CreateAttachmentResponse(
            string fileName =
                "documento.pdf",
            string contentType =
                "application/pdf",
            string? description =
                "Documento de respaldo")
    {
        return new LoanGuaranteeAttachmentResponse
        {
            Id =
                Guid.NewGuid(),

            LoanGuaranteeId =
                Guid.NewGuid(),

            UploadedByAppUserId =
                Guid.NewGuid(),

            OriginalFileName =
                fileName,

            ContentType =
                contentType,

            FileSize =
                1024,

            Description =
                description,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    private static HttpResponseMessage
        JsonResponse<T>(
            HttpStatusCode statusCode,
            T value)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }

    private static HttpResponseMessage
        ApiError(
            string message,
            HttpStatusCode statusCode =
                HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    new
                    {
                        message
                    })
        };
    }

    private static T DeserializeBody<T>(
        string body)
    {
        var value =
            JsonSerializer.Deserialize<T>(
                body,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web));

        Assert.NotNull(
            value);

        return value;
    }
}