namespace Acm.Application.Features.AccessControlFeatures.Outcomes;

public enum LoginBadOutcome : byte
{
    UserNotFound = 1,
    PasswordNotMatched,
    InvalidUserStatus
}
