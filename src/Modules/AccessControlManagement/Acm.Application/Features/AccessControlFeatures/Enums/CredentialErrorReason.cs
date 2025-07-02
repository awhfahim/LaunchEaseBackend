namespace Acm.Application.Features.AccessControlFeatures.Enums;

public enum CredentialErrorReason : byte
{
    UserNotFound = 1,
    PasswordNotMatched,
    ProfileAlreadyConfirmed
}
