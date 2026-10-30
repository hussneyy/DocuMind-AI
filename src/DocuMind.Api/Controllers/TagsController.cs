using DocuMind.Api.Contracts.Tags;
using DocuMind.Api.Extensions;
using DocuMind.Application.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tags = await tagService.ListAsync(User.GetUserId(), ct);
        return Ok(tags);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken ct)
    {
        var tag = await tagService.CreateAsync(User.GetUserId(), request.Name, ct);
        return Ok(tag);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tagService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
