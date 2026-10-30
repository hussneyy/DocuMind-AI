namespace DocuMind.Application.Auditing;

public enum AuditEventType
{
    UserRegistered,
    UserLoggedIn,
    LoginFailed,
    UserLoggedOut,
    DocumentUploaded,
    DocumentDeleted,
    ConversationDeleted,
    DisplayNameChanged,
    TagCreated,
    TagDeleted,
}
