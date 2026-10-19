using DocuMind.Api.Contracts.Chat;
using DocuMind.Api.Extensions;
using DocuMind.Application.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<IActionResult> Ask(AskRequestDto request, CancellationToken ct)
    {
        var result = await chatService.AskAsync(User.GetUserId(), request.ConversationId, request.Question, request.DocumentIds, ct);
        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> ListConversations(CancellationToken ct)
    {
        var conversations = await chatService.ListConversationsAsync(User.GetUserId(), ct);
        return Ok(conversations);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken ct)
    {
        var conversation = await chatService.GetConversationAsync(User.GetUserId(), id, ct);
        return Ok(conversation);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
    {
        await chatService.DeleteConversationAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
