namespace Booking.Middleware.Api.DTOs;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static ApiResponse<T> Exitoso(T data, string message = "Operación exitosa", int statusCode = 200) =>
        new() { Success = true, StatusCode = statusCode, Message = message, Data = data };

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa", int statusCode = 200) =>
        Exitoso(data, message, statusCode);
}
