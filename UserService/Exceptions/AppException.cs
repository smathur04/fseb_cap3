namespace UserService.Exceptions;

/// <summary>
/// Application-level domain exception that carries an error code and HTTP status.
/// </summary>
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AppException(string errorCode, string message, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    // Convenience factories
    public static AppException ValidationError(string message) =>
        new("VALIDATION_ERROR", message, 400);

    public static AppException AuthenticationFailed(string message = "Invalid email or password") =>
        new("AUTHENTICATION_FAILED", message, 401);

    public static AppException NotFound(string message) =>
        new("NOT_FOUND", message, 404);

    public static AppException MembershipInactive(string message) =>
        new("MEMBERSHIP_INACTIVE", message, 403);
}
