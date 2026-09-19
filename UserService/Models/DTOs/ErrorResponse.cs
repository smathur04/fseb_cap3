namespace UserService.Models.DTOs;

/// <summary>
/// Standard error envelope returned by all error paths.
/// Extra fields can be supplied via the <see cref="Details"/> dictionary.
/// </summary>
public class ErrorResponse
{
    public string Error { get; init; } = "";
    public string Message { get; init; } = "";
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");

    /// <summary>Optional extra fields (e.g. field-level validation errors).</summary>
    public Dictionary<string, object>? Details { get; init; }

    public ErrorResponse() { }

    public ErrorResponse(string error, string message, Dictionary<string, object>? details = null)
    {
        Error = error;
        Message = message;
        Timestamp = DateTime.UtcNow.ToString("o");
        Details = details;
    }
}
