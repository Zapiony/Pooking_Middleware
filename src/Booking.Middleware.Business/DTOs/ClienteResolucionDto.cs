namespace Booking.Middleware.Business.DTOs;

/// <summary>
/// Snapshot del cliente resuelto para enrichment de una reserva.
/// Retornado por IClienteGrpcClient.ObtenerClientePorGuidAsync().
/// </summary>
public sealed class ClienteResolucionDto
{
    public bool   Found        { get; init; }
    public string GuidCliente  { get; init; } = string.Empty;
    public string Nombre       { get; init; } = string.Empty;
    public string Apellido     { get; init; } = string.Empty;
    public string Correo       { get; init; } = string.Empty;
    public string Telefono     { get; init; } = string.Empty;
    public string Estado       { get; init; } = string.Empty;
    public string Mensaje      { get; init; } = string.Empty;
}
