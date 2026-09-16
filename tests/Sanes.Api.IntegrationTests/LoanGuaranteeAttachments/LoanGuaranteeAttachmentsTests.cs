using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.LoanGuaranteeAttachments;

public class LoanGuaranteeAttachmentsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private const long MaximumFileSize =
        10L * 1024L * 1024L;

    private readonly CustomWebApplicationFactory
        _factory;

    public LoanGuaranteeAttachmentsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // VALID FILE TYPES
    // ============================================================

    [Fact]
    public async Task UploadPdf_WithValidFile_ReturnsCreated()
    {
        var scenario =
            await CreateScenarioAsync();

        var bytes =
            CreatePdfBytes();

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                bytes,
                "garantia.pdf",
                "application/pdf",
                "Documento PDF de garantía");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var attachment =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeAttachmentResponse>();

        Assert.NotNull(attachment);

        Assert.NotEqual(
            Guid.Empty,
            attachment.Id);

        Assert.Equal(
            scenario.Loan.Guarantee!.Id,
            attachment.LoanGuaranteeId);

        Assert.Equal(
            "garantia.pdf",
            attachment.OriginalFileName);

        Assert.Equal(
            "application/pdf",
            attachment.ContentType);

        Assert.Equal(
            bytes.LongLength,
            attachment.FileSize);

        Assert.Equal(
            "Documento PDF de garantía",
            attachment.Description);

        Assert.Equal(
            scenario.Context.AdministratorId,
            attachment.UploadedByAppUserId);
    }

    [Fact]
    public async Task UploadJpeg_WithValidFile_ReturnsCreated()
    {
        var scenario =
            await CreateScenarioAsync();

        var bytes =
            CreateJpegBytes();

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                bytes,
                "vehiculo.jpg",
                "image/jpeg");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var attachment =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeAttachmentResponse>();

        Assert.NotNull(attachment);

        Assert.Equal(
            "vehiculo.jpg",
            attachment.OriginalFileName);

        Assert.Equal(
            "image/jpeg",
            attachment.ContentType);

        Assert.Equal(
            bytes.LongLength,
            attachment.FileSize);
    }

    [Fact]
    public async Task UploadPng_WithValidFile_ReturnsCreated()
    {
        var scenario =
            await CreateScenarioAsync();

        var bytes =
            CreatePngBytes();

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                bytes,
                "matricula.png",
                "image/png");

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var attachment =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeAttachmentResponse>();

        Assert.NotNull(attachment);

        Assert.Equal(
            "matricula.png",
            attachment.OriginalFileName);

        Assert.Equal(
            "image/png",
            attachment.ContentType);
    }

    // ============================================================
    // FILE VALIDATION
    // ============================================================

    [Fact]
    public async Task Upload_WithMismatchedExtensionAndContentType_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "garantia.jpg",
                "application/pdf");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_FakePdfSignature_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var fakePdf =
            Encoding.UTF8.GetBytes(
                "This is not really a PDF file.");

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                fakePdf,
                "garantia.pdf",
                "application/pdf");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_FileLargerThan10Mb_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var bytes =
            new byte[MaximumFileSize + 1];

        /*
         * Firma PNG válida.
         *
         * Queremos que falle específicamente por tamaño,
         * no por firma.
         */
        var signature =
            CreatePngBytes();

        Array.Copy(
            signature,
            bytes,
            signature.Length);

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                bytes,
                "archivo-grande.png",
                "image/png");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_EleventhActiveAttachment_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        for (var index = 1;
             index <= 10;
             index++)
        {
            var response =
                await UploadAsync(
                    scenario.Context.Client,
                    scenario.Loan.Id,
                    CreatePdfBytes(),
                    $"documento-{index}.pdf",
                    "application/pdf");

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }

        var eleventhResponse =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "documento-11.pdf",
                "application/pdf");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            eleventhResponse.StatusCode);
    }

    // ============================================================
    // METADATA
    // ============================================================

    [Fact]
    public async Task GetAll_ReturnsActiveAttachments()
    {
        var scenario =
            await CreateScenarioAsync();

        var first =
            await UploadAndReadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "documento-a.pdf",
                "application/pdf");

        var second =
            await UploadAndReadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePngBytes(),
                "documento-b.png",
                "image/png");

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var attachments =
            await response.Content
                .ReadFromJsonAsync<
                    List<LoanGuaranteeAttachmentResponse>>();

        Assert.NotNull(attachments);

        Assert.Equal(
            2,
            attachments.Count);

        Assert.Contains(
            attachments,
            x => x.Id == first.Id);

        Assert.Contains(
            attachments,
            x => x.Id == second.Id);
    }

    [Fact]
    public async Task GetById_ReturnsAttachmentMetadata()
    {
        var scenario =
            await CreateScenarioAsync();

        var created =
            await UploadAndReadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "contrato.pdf",
                "application/pdf",
                "Contrato firmado");

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{created.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var attachment =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeAttachmentResponse>();

        Assert.NotNull(attachment);

        Assert.Equal(
            created.Id,
            attachment.Id);

        Assert.Equal(
            "contrato.pdf",
            attachment.OriginalFileName);

        Assert.Equal(
            "application/pdf",
            attachment.ContentType);

        Assert.Equal(
            "Contrato firmado",
            attachment.Description);
    }

    // ============================================================
    // DOWNLOAD
    // ============================================================

    [Fact]
    public async Task GetContent_ReturnsExactStoredBytes()
    {
        var scenario =
            await CreateScenarioAsync();

        var originalBytes =
            CreatePdfBytes();

        var attachment =
            await UploadAndReadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                originalBytes,
                "original.pdf",
                "application/pdf");

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{attachment.Id}/content");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/pdf",
            response.Content.Headers
                .ContentType?
                .MediaType);

        var downloadedBytes =
            await response.Content
                .ReadAsByteArrayAsync();

        Assert.Equal(
            originalBytes,
            downloadedBytes);
    }

    // ============================================================
    // SOFT DELETE
    // ============================================================

    [Fact]
    public async Task Delete_SoftDeletesMetadataAndKeepsPhysicalFile()
    {
        var scenario =
            await CreateScenarioAsync();

        var attachment =
            await UploadAndReadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "evidencia.pdf",
                "application/pdf");

        var guaranteeId =
            scenario.Loan.Guarantee!.Id;

        var storageRoot =
            Environment.GetEnvironmentVariable(
                "FileStorage__RootPath");

        Assert.False(
            string.IsNullOrWhiteSpace(
                storageRoot));

        var guaranteeDirectory =
            Path.Combine(
                storageRoot!,
                "loan-guarantees",
                scenario.Context.TenantId
                    .ToString("N"),
                guaranteeId
                    .ToString("N"));

        Assert.True(
            Directory.Exists(
                guaranteeDirectory));

        var storedFiles =
            Directory.GetFiles(
                guaranteeDirectory);

        var storedFile =
            Assert.Single(
                storedFiles);

        Assert.True(
            File.Exists(
                storedFile));

        var deleteResponse =
            await scenario.Context.Client.DeleteAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{attachment.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        /*
         * Metadata eliminada lógicamente.
         */
        var metadataResponse =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{attachment.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            metadataResponse.StatusCode);

        /*
         * Tampoco debe poder descargarse mediante API.
         */
        var contentResponse =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{attachment.Id}/content");

        Assert.Equal(
            HttpStatusCode.NotFound,
            contentResponse.StatusCode);

        /*
         * Pero el archivo físico permanece para auditoría.
         */
        Assert.True(
            File.Exists(
                storedFile));

        /*
         * Y no aparece en el listado normal.
         */
        var listResponse =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments");

        listResponse
            .EnsureSuccessStatusCode();

        var attachments =
            await listResponse.Content
                .ReadFromJsonAsync<
                    List<LoanGuaranteeAttachmentResponse>>();

        Assert.NotNull(attachments);
        Assert.Empty(attachments);
    }

    // ============================================================
    // LOAN / GUARANTEE RULES
    // ============================================================

    [Fact]
    public async Task Upload_LoanWithoutGuarantee_ReturnsNotFound()
    {
        var context =
            await CreateContextAsync();

        var loan =
            await CreateLoanAsync(
                context.Client,
                includeGuarantee: false);

        Assert.Null(
            loan.Guarantee);

        var response =
            await UploadAsync(
                context.Client,
                loan.Id,
                CreatePdfBytes(),
                "garantia.pdf",
                "application/pdf");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Upload_CancelledLoan_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var cancelResponse =
            await scenario.Context.Client.PatchAsync(
                $"/api/loans/{scenario.Loan.Id}/cancel",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            cancelResponse.StatusCode);

        var response =
            await UploadAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                CreatePdfBytes(),
                "posterior-cancelacion.pdf",
                "application/pdf");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task Collector_CannotAccessAttachments_ReturnsForbidden()
    {
        var scenario =
            await CreateScenarioAsync();

        var collectorUsername =
            $"collector_{Guid.NewGuid():N}";

        var createCollectorResponse =
            await scenario.Context.Client
                .PostAsJsonAsync(
                    "/api/app-users",
                    new
                    {
                        name =
                            "Collector Attachments Test",

                        username =
                            collectorUsername,

                        password =
                            TestAuthenticationHelper
                                .DefaultPassword,

                        email =
                            $"collector-{Guid.NewGuid():N}@test.local",

                        phone =
                            "8095552000",

                        role =
                            AppUserRole.Collector
                    });

        createCollectorResponse
            .EnsureSuccessStatusCode();

        using var anonymousClient =
            _factory.CreateClient();

        var collectorToken =
            await TestAuthenticationHelper.LoginAsync(
                anonymousClient,
                scenario.Context.TenantId,
                collectorUsername,
                TestAuthenticationHelper
                    .DefaultPassword);

        using var collectorClient =
            TestAuthenticationHelper
                .CreateAuthenticatedClient(
                    _factory,
                    collectorToken);

        var response =
            await collectorClient.GetAsync(
                $"/api/loans/{scenario.Loan.Id}" +
                "/guarantee/attachments");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task CrossTenant_GetById_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2Scenario =
            await CreateScenarioAsync();

        var attachment =
            await UploadAndReadAsync(
                tenant2Scenario.Context.Client,
                tenant2Scenario.Loan.Id,
                CreatePdfBytes(),
                "tenant-two.pdf",
                "application/pdf");

        /*
         * El token pertenece a Tenant 1.
         * Loan y attachment pertenecen a Tenant 2.
         */
        var response =
            await tenant1.Client.GetAsync(
                $"/api/loans/{tenant2Scenario.Loan.Id}" +
                "/guarantee/attachments/" +
                $"{attachment.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<AttachmentScenario>
        CreateScenarioAsync()
    {
        var context =
            await CreateContextAsync();

        var loan =
            await CreateLoanAsync(
                context.Client,
                includeGuarantee: true);

        Assert.NotNull(
            loan.Guarantee);

        return new AttachmentScenario
        {
            Context =
                context,

            Loan =
                loan
        };
    }

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            bool includeGuarantee)
    {
        var investor =
            await CreateInvestorAsync(
                client);

        var loanClient =
            await CreateClientAsync(
                client);

        var request =
            new CreateLoanRequest
            {
                InvestorId =
                    investor.Id,

                ClientId =
                    loanClient.Id,

                PrincipalAmount =
                    1000m,

                InstallmentAmount =
                    100m,

                TotalInstallments =
                    13,

                PaymentFrequency =
                    PaymentFrequency.Weekly,

                StartDate =
                    DateTime.UtcNow.Date,

                Notes =
                    "Loan para pruebas de adjuntos",

                Guarantee =
                    includeGuarantee
                        ? new CreateLoanGuaranteeRequest
                        {
                            Type =
                                LoanGuaranteeType.Collateral,

                            Reference =
                                $"GAR-{Guid.NewGuid():N}",

                            Description =
                                "Garantía creada para integration test"
                        }
                        : null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/loans",
                request);

        response
            .EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(
            loan);

        return loan;
    }

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client)
    {
        var request =
            new CreateInvestorRequest
            {
                Name =
                    $"Investor Attachments {Guid.NewGuid():N}",

                Phone =
                    "8095551000",

                Identification =
                    $"ATT-{Guid.NewGuid():N}"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                request);

        response
            .EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(
            investor);

        return investor;
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client)
    {
        var request =
            new CreateClientRequest
            {
                FirstName =
                    "Cliente",

                LastName =
                    $"Attachments {Guid.NewGuid():N}",

                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Address =
                    "Santiago"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                request);

        response
            .EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(
            createdClient);

        return createdClient;
    }

    private static async Task<HttpResponseMessage>
        UploadAsync(
            HttpClient client,
            Guid loanId,
            byte[] bytes,
            string fileName,
            string contentType,
            string? description = null)
    {
        using var form =
            new MultipartFormDataContent();

        var fileContent =
            new ByteArrayContent(
                bytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        form.Add(
            fileContent,
            "file",
            fileName);

        if (description is not null)
        {
            form.Add(
                new StringContent(
                    description),
                "description");
        }

        return await client.PostAsync(
            $"/api/loans/{loanId}" +
            "/guarantee/attachments",
            form);
    }

    private static async Task<
        LoanGuaranteeAttachmentResponse>
        UploadAndReadAsync(
            HttpClient client,
            Guid loanId,
            byte[] bytes,
            string fileName,
            string contentType,
            string? description = null)
    {
        var response =
            await UploadAsync(
                client,
                loanId,
                bytes,
                fileName,
                contentType,
                description);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var attachment =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeAttachmentResponse>();

        Assert.NotNull(
            attachment);

        return attachment;
    }

    private static byte[]
        CreatePdfBytes()
    {
        return Encoding.ASCII.GetBytes(
            "%PDF-1.4\n" +
            "1 0 obj\n" +
            "<< /Type /Catalog >>\n" +
            "endobj\n" +
            "%%EOF");
    }

    private static byte[]
        CreateJpegBytes()
    {
        return
        [
            0xFF,
            0xD8,
            0xFF,
            0xE0,
            0x00,
            0x10,
            0x4A,
            0x46,
            0x49,
            0x46,
            0x00,
            0x01,
            0xFF,
            0xD9
        ];
    }

    private static byte[]
        CreatePngBytes()
    {
        return
        [
            0x89,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A,
            0x00,
            0x00,
            0x00,
            0x00
        ];
    }

    private sealed class AttachmentScenario
    {
        public TestTenantContext Context
            { get; init; } = null!;

        public LoanResponse Loan
            { get; init; } = null!;
    }
}