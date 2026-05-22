namespace Booking.Middleware.Api.DTOs;

public sealed class ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiErrorResponse Crear(string message, int statusCode, IReadOnlyList<string>? errors = null, string? traceId = null) =>
        new()
        {
            StatusCode = statusCode,
            Message = message,
            TraceId = traceId,
            Errors = errors ?? Array.Empty<string>()
        };
}
