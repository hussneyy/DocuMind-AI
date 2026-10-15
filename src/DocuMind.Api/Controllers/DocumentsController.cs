using DocuMind.Api.Extensions;
using DocuMind.Application.Documents;
using DocuMind.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null)
        {
            return BadRequest(new { errors = new[] { "No file was provided." } });
        }

        await using var stream = file.OpenReadStream();
        var dto = await documentService.UploadAsync(User.GetUserId(), stream, file.FileName, file.Length, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DocumentProcessingStatus? status = null,
        [FromQuery] string sortBy = "uploadedAt",
        [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        var query = new DocumentListQuery(
            page,
            pageSize,
            search,
            status,
            string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase) ? DocumentSortField.Name : DocumentSortField.UploadedAt,
            sortDesc);

        var result = await documentService.ListAsync(User.GetUserId(), query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await documentService.GetAsync(User.GetUserId(), id, ct);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var dto = await documentService.GetAsync(User.GetUserId(), id, ct);
        return Ok(new { dto.ProcessingStatus, dto.ProcessingError });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await documentService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var dto = await documentService.RetryAsync(User.GetUserId(), id, ct);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/chunks")]
    public async Task<IActionResult> GetChunks(Guid id, CancellationToken ct)
    {
        var chunks = await documentService.ListChunksAsync(User.GetUserId(), id, ct);
        return Ok(chunks);
    }
}
