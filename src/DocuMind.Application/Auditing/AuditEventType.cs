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
    ProfileUpdated,
    AvatarUpdated,
    AvatarRemoved,
    PasswordResetRequested,
    PasswordResetCompleted,
    TagCreated,
    TagDeleted,
}
