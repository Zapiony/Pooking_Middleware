namespace Booking.Middleware.Business.DTOs;

/// <summary>
/// Snapshot del servicio resuelto para una reserva.
/// Retornado por CrossDomainResolverService.ResolverServicioAsync().
/// </summary>
public sealed class ServicioResolucionDto
{
    public bool   Found            { get; init; }
    public string GuidServicio     { get; init; } = string.Empty;
    public string RazonSocial      { get; init; } = string.Empty;
    public string NombreComercial  { get; init; } = string.Empty;
    public string TipoServicio     { get; init; } = string.Empty;
    public string Estado           { get; init; } = string.Empty;
    public string CorreoContacto   { get; init; } = string.Empty;
    public string TelefonoContacto { get; init; } = string.Empty;
    public string LogoUrl          { get; init; } = string.Empty;
    public string Mensaje          { get; init; } = string.Empty;
}
