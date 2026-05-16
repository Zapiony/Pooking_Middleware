using Booking.Middleware.Business.Exceptions;
using Grpc.Core;
using System.Net;
using System.Text.Json;

namespace Booking.Middleware.Api.Middleware;

/// <summary>
/// Middleware global de manejo de excepciones para llamadas HTTP (health checks, endpoint raíz).
/// Las excepciones de los servicios gRPC se manejan directamente en cada GrpcService
/// con RpcException — este middleware cubre el resto del pipeline HTTP.
/// </summary>
public sealed class ExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en pipeline HTTP: {Mensaje}", ex.Message);
            await EscribirRespuestaAsync(context, ex);
        }
    }

    private static async Task EscribirRespuestaAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, mensaje) = exception switch
        {
            ServiceUnavailableException sue =>
                ((int)HttpStatusCode.ServiceUnavailable,
                 $"Servicio externo no disponible ({sue.ServicioAfectado}): {sue.Message}"),

            MiddlewareException me =>
                ((int)HttpStatusCode.BadRequest, me.Message),

            _ =>
                ((int)HttpStatusCode.InternalServerError,
                 "Error interno del servidor.")
        };

        context.Response.StatusCode = statusCode;

        var body = new
        {
            success = false,
            statusCode,
            mensaje,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, JsonOptions));
    }
}
