using DocuMind.Api.Contracts.Search;
using DocuMind.Api.Extensions;
using DocuMind.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Search(SearchRequestDto request, CancellationToken ct)
    {
        if (!Enum.TryParse<SearchMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return BadRequest(new { errors = new[] { $"Invalid search mode '{request.Mode}'. Use Semantic, Keyword, or Hybrid." } });
        }

        var searchRequest = new SearchRequest(request.Query, request.DocumentIds, mode, request.TopK);
        var results = await searchService.SearchAsync(User.GetUserId(), searchRequest, ct);
        return Ok(results);
    }
}
