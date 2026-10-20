using DocuMind.Application.Auditing;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Auditing;

/// <summary>
/// Writes to a distinctly-named logger category ("DocuMind.Audit") rather than mixing security
/// events into whatever category the calling class would otherwise use — an operator can filter
/// or route by that category (e.g. in appsettings' Serilog MinimumLevel:Override section) without
/// needing a separate audit database.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private const string CategoryName = "DocuMind.Audit";

    private readonly ILogger _logger;

    public AuditLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(CategoryName);
    }

    public void Log(AuditEventType eventType, Guid? userId, string details)
    {
        _logger.LogInformation("AUDIT {EventType} user={UserId} — {Details}", eventType, userId, details);
    }
}
