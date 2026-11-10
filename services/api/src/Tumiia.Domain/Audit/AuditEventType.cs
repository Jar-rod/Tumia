namespace Tumiia.Domain.Audit;

/// <summary>The security and document events written to the audit log.</summary>
public enum AuditEventType
{
    Registered,
    LoginSucceeded,
    LoginFailed,
    LockedOut,
    PasswordResetRequested,
    PasswordReset,
    RefreshTokenReuseDetected,
    LoggedOut,
    DocumentUploadRequested,
    DocumentUploaded,
    DocumentAnalysed,
    DocumentAnalysisFailed,
}
