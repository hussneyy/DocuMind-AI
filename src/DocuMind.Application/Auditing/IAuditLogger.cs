namespace DocuMind.Application.Auditing;

/// <summary>
/// Records security-relevant events distinctly from ordinary application logs, so they can be
/// filtered, routed to a separate sink, or retained on a different schedule without touching
/// general log noise. Deliberately not a persisted database table — at this project's scale, a
/// distinctly-tagged log stream is enough; a dedicated audit store would be unjustified complexity.
/// </summary>
public interface IAuditLogger
{
    /// <param name="userId">The acting user, or null for an event with no authenticated subject (e.g. a failed login).</param>
    /// <param name="details">Short, human-readable context — never document contents or secrets.</param>
    void Log(AuditEventType eventType, Guid? userId, string details);
}
