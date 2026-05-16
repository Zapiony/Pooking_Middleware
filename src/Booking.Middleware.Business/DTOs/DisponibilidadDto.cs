namespace Booking.Middleware.Business.DTOs;

/// <summary>
/// Resultado de validar la disponibilidad de un servicio para una fecha.
/// </summary>
public sealed class DisponibilidadDto
{
    public bool   Disponible { get; init; }
    public string Motivo     { get; init; } = string.Empty;
}
