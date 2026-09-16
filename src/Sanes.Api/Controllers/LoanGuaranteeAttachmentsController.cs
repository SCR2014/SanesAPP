using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/loans/{loanId:guid}/guarantee/attachments")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class LoanGuaranteeAttachmentsController
    : ControllerBase
{
    private readonly ILoanGuaranteeAttachmentService
        _attachmentService;

    private readonly ICurrentUserService
        _currentUserService;

    public LoanGuaranteeAttachmentsController(
        ILoanGuaranteeAttachmentService attachmentService,
        ICurrentUserService currentUserService)
    {
        _attachmentService =
            attachmentService;

        _currentUserService =
            currentUserService;
    }

    // ============================================================
    // LIST
    // ============================================================

    [HttpGet]
    [ProducesResponseType(
        typeof(List<LoanGuaranteeAttachmentResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<List<LoanGuaranteeAttachmentResponse>>>
        GetAll(
            Guid loanId,
            CancellationToken cancellationToken)
    {
        if (loanId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId must be a valid identifier."
                });
        }

        var result =
            await _attachmentService.GetAllAsync(
                _currentUserService.TenantId,
                loanId,
                cancellationToken);

        /*
         * null significa:
         *
         * - préstamo inexistente para este Tenant, o
         * - préstamo sin garantía.
         */
        if (result is null)
        {
            return NotFound();
        }

        return Ok(
            result);
    }

    // ============================================================
    // METADATA
    // ============================================================

    [HttpGet("{attachmentId:guid}")]
    [ProducesResponseType(
        typeof(LoanGuaranteeAttachmentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<LoanGuaranteeAttachmentResponse>>
        GetById(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken)
    {
        if (
            loanId == Guid.Empty ||
            attachmentId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId and AttachmentId must be valid identifiers."
                });
        }

        var result =
            await _attachmentService.GetByIdAsync(
                _currentUserService.TenantId,
                loanId,
                attachmentId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(
            result);
    }

    // ============================================================
    // CONTENT
    // ============================================================

    [HttpGet("{attachmentId:guid}/content")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        GetContent(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken)
    {
        if (
            loanId == Guid.Empty ||
            attachmentId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId and AttachmentId must be valid identifiers."
                });
        }

        try
        {
            var result =
                await _attachmentService.OpenContentAsync(
                    _currentUserService.TenantId,
                    loanId,
                    attachmentId,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            Response.ContentLength =
                result.FileSize;

            return File(
                result.Content,
                result.ContentType,
                result.FileName,
                enableRangeProcessing: true);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message =
                        ex.Message
                });
        }
    }

    // ============================================================
    // UPLOAD
    // ============================================================

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        11 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(LoanGuaranteeAttachmentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<LoanGuaranteeAttachmentResponse>>
        Upload(
            Guid loanId,
            [FromForm] IFormFile file,
            [FromForm] string? description,
            CancellationToken cancellationToken)
    {
        if (loanId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId must be a valid identifier."
                });
        }

        if (file is null)
        {
            return BadRequest(
                new
                {
                    message =
                        "A file is required."
                });
        }

        try
        {
            await using var stream =
                file.OpenReadStream();

            var result =
                await _attachmentService.UploadAsync(
                    _currentUserService.TenantId,
                    _currentUserService.AppUserId,
                    loanId,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream,
                    description,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    loanId,
                    attachmentId =
                        result.Id
                },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message =
                        ex.Message
                });
        }
    }

    // ============================================================
    // SOFT DELETE
    // ============================================================

    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        Delete(
            Guid loanId,
            Guid attachmentId,
            CancellationToken cancellationToken)
    {
        if (
            loanId == Guid.Empty ||
            attachmentId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId and AttachmentId must be valid identifiers."
                });
        }

        var deleted =
            await _attachmentService.DeleteAsync(
                _currentUserService.TenantId,
                _currentUserService.AppUserId,
                loanId,
                attachmentId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}