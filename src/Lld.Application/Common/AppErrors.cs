using Lld.Domain.Common;

namespace Lld.Application.Common;

/// <summary>
/// Errors raised by use cases. Codes are stable; the API maps them to HTTP status codes.
/// Sign-in errors are deliberately generic so they don't reveal which emails have accounts.
/// </summary>
public static class AppErrors
{
    public static readonly Error InvalidCredentials =
        new("auth.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error LockedOut =
        new("auth.locked_out", "Too many failed attempts. Try again in 15 minutes.");

    public static readonly Error SessionInvalid =
        new("auth.session_invalid", "Your session has ended. Please sign in again.");

    public static readonly Error RefreshTokenReused =
        new("auth.refresh_token_reused", "This session was signed out for your security. Please sign in again.");

    public static readonly Error CurrentPasswordIncorrect =
        new("auth.current_password_incorrect", "Your current password is incorrect.");

    public static readonly Error UserNotFound = new("user.not_found", "No user with that email.");

    public static readonly Error UserAlreadyExists = new("user.already_exists", "A user with that email already exists.");

    public static readonly Error UnknownRole = Error.Validation("role.unknown", "Role must be Admin or Learner.");

    public static readonly Error AccessRequestNotFound =
        new("access_request.not_found", "No access request with that id.");

    public static readonly Error ActivityDayInFuture =
        Error.Validation("progress.day", "A study day can't be later than tomorrow.");

    public static readonly Error TooManyActivityDays =
        Error.Validation("progress.too_many_days", "Too many study days in one request.");
}
