namespace DocuMind.Application.Auth;

/// <summary>
/// The result of requesting a password reset. <see cref="ResetLink"/> is populated only when the
/// account exists — this project has no email sending configured, so the link is surfaced
/// directly in the API response for the frontend to display (demo-mode only; a production
/// deployment would email this link instead of returning it).
/// </summary>
public record PasswordResetRequestResult(bool AccountExists, string? ResetLink);
