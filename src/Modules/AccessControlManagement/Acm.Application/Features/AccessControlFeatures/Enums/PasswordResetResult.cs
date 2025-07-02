namespace Acm.Application.Features.AccessControlFeatures.Enums;

public enum PasswordResetResult : byte
{
    UserNotFound = 1,
    ProfileNotConfirmed,
    SameAsOldPassword,
    InvalidToken,
    Ok
}
