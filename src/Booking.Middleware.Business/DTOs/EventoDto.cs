namespace Booking.Middleware.Business.DTOs;

/// <summary>
/// Datos de un evento de auditoría que el Middleware recibe de un microservicio
/// y reenvía al servicio de Auditoria vía gRPC.
/// </summary>
public sealed class EventoDto
{
    public string IdCorrelacion   { get; init; } = string.Empty;
    public string TablaAfectada   { get; init; } = string.Empty;
    public string EsquemaAfectado { get; init; } = "booking";
    public int    Operacion       { get; init; }   // 0=INSERT, 1=UPDATE, 2=DELETE
    public string IdRegistro      { get; init; } = string.Empty;
    public string DatosAnteriores { get; init; } = string.Empty;
    public string DatosNuevos     { get; init; } = string.Empty;
    public string Usuario         { get; init; } = string.Empty;
    public string Ip              { get; init; } = string.Empty;
    public string ServicioOrigen  { get; init; } = string.Empty;
    public string EquipoOrigen    { get; init; } = string.Empty;
}
