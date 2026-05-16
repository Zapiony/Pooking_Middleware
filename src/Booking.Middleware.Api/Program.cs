using Booking.Middleware.Api.Extensions;
using Booking.Middleware.Api.Middleware;

// ── HTTP/2 sin TLS en desarrollo local ───────────────────────────────────────
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// ── gRPC Server (este proceso como servidor) ──────────────────────────────────
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ── gRPC Clients + Interfaces de DataAccess ───────────────────────────────────
builder.Services.AddMiddlewareGrpcClients(builder.Configuration);

// ── DataManagement + Business ─────────────────────────────────────────────────
builder.Services.AddMiddlewareServices();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();

// ── Endpoints gRPC Server ─────────────────────────────────────────────────────
app.MapMiddlewareGrpcServices();

// ── Endpoints HTTP ────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    servicio = "Booking.Middleware",
    version  = "1.0",
    estado   = "activo"
}));

app.Run();
