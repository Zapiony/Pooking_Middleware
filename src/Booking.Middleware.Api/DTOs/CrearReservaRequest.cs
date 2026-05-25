namespace Booking.Middleware.Api.DTOs;

public sealed class CrearReservaRequest
{
    public Guid GuidCliente { get; init; }
    public Guid GuidServicioRef { get; init; }
    public string NombreServicioSnap { get; init; } = string.Empty;
    public string TipoServicioSnap { get; init; } = string.Empty;
    public string NombreProveedor { get; init; } = string.Empty;
    public string IdReservaExterna { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public string CanalOrigen { get; init; } = string.Empty;
    public decimal MontoTotal { get; init; }
    public string Moneda { get; init; } = string.Empty;
    public string Observaciones { get; init; } = string.Empty;
}
