namespace Booking.Middleware.Business.Exceptions;

/// <summary>
/// Excepción base del Middleware para errores de lógica de negocio.
/// </summary>
public class MiddlewareException : Exception
{
    public MiddlewareException(string message) : base(message) { }
    public MiddlewareException(string message, Exception innerException)
        : base(message, innerException) { }
}
