namespace Booking.Middleware.Business.Exceptions;

/// <summary>
/// Se lanza cuando un servicio externo (Auth, Servicio, Auditoria) no está
/// disponible o supera el tiempo de espera. Permite aplicar circuit-breaker.
/// </summary>
public sealed class ServiceUnavailableException : MiddlewareException
{
    public string ServicioAfectado { get; }

    public ServiceUnavailableException(string servicioAfectado, string mensaje)
        : base(mensaje)
    {
        ServicioAfectado = servicioAfectado;
    }

    public ServiceUnavailableException(string servicioAfectado, string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
        ServicioAfectado = servicioAfectado;
    }
}
