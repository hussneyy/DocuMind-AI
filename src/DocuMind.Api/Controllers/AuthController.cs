using DocuMind.Api.Contracts.Auth;
using DocuMind.Api.Extensions;
using DocuMind.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CurrentUserDto>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password, request.DisplayName, ct);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.User);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, ct);
        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.User);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await authService.LogoutAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
    {
        var user = await authService.GetCurrentUserAsync(User.GetUserId(), ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await authService.UpdateProfileAsync(
            User.GetUserId(), request.DisplayName, request.Bio, request.Location, request.PhoneNumber, ct);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.User);
    }

    [HttpPost("me/avatar")]
    [Authorize]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken ct)
    {
        if (file is null)
        {
            return BadRequest(new { errors = new[] { "No file was provided." } });
        }

        var metadataError = AvatarUploadValidator.ValidateMetadata(file.ContentType, file.Length);
        if (metadataError is not null)
        {
            return BadRequest(new { errors = new[] { metadataError } });
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[Math.Min(12, (int)file.Length)];
        var read = await stream.ReadAsync(header, ct);
        stream.Position = 0;

        if (read < header.Length || !AvatarUploadValidator.HasValidImageSignature(header))
        {
            return BadRequest(new { errors = new[] { "The file is not a valid PNG, JPEG, or WEBP image." } });
        }

        var dto = await authService.UpdateAvatarAsync(User.GetUserId(), stream, file.ContentType, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("me/avatar")]
    [Authorize]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var dto = await authService.RemoveAvatarAsync(User.GetUserId(), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("me/avatar")]
    [Authorize]
    public async Task<IActionResult> GetAvatar(CancellationToken ct)
    {
        var result = await authService.OpenAvatarAsync(User.GetUserId(), ct);
        return result is null ? NotFound() : File(result.Value.Content, result.Value.ContentType);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await authService.RequestPasswordResetAsync(request.Email, ct);
        return Ok(new { result.AccountExists, result.ResetLink });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, ct);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }
}
