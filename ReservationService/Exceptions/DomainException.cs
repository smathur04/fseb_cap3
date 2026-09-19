namespace ReservationService.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public object? ExtraData { get; }

    public DomainException(string errorCode, string message, int statusCode = 400, object? extraData = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        ExtraData = extraData;
    }
}
