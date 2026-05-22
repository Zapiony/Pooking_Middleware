namespace Booking.Middleware.Api.DTOs;

public class BusquedaRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
