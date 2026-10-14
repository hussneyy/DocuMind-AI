namespace DocuMind.Api.Middleware;

/// <summary>
/// Lightweight CSRF defense for cookie-based authentication: state-changing requests must carry a
/// custom header. A cross-site HTML form cannot add custom headers, and a cross-origin script
/// request that tries to would trigger a CORS preflight our policy rejects for any origin other
/// than the configured frontend. This is simpler than antiforgery tokens for a same-site SPA + API
/// split across local ports, at the cost of only protecting origins we don't also trust via CORS.
/// </summary>
public class CsrfProtectionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> UnsafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        if (UnsafeMethods.Contains(context.Request.Method)
            && !context.Request.Headers.ContainsKey("X-Requested-With"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Missing required X-Requested-With header." });
            return;
        }

        await next(context);
    }
}
