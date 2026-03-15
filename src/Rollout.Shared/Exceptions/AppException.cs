namespace Rollout.Shared.Exceptions;

public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string? ErrorDetail { get; }

    public AppException(int statusCode, string errorCode, string? errorDetail = null)
        : base(errorDetail ?? errorCode)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorDetail = errorDetail;
    }
}