# Booking.Middleware

Bus de eventos y resolución cross-dominio para el sistema **Pooking API**.

## Arquitectura

```
 Cliente / Auth / Servicio
       │ gRPC (EventBus + Resolver)
       ▼
  Booking.Middleware          ← este proyecto
       │ gRPC client
       ├──→ Auditoria (registro de eventos)
       ├──→ Auth      (resolución de usuarios)
       └──→ Servicio  (catálogo + disponibilidad)
```

## Responsabilidades

| Servicio gRPC            | Tipo       | Propósito |
|--------------------------|------------|-----------|
| `EventBusGrpcService`    | Asíncrono  | Recibe eventos de escritura → los reenvía a Auditoria |
| `ResolverGrpcService`    | Síncrono   | Resuelve datos cross-dominio sin acoplar microservicios |

## Estructura de capas

```
src/
├── Booking.Middleware.Api/             ← gRPC servers, Program.cs, Extensions
├── Booking.Middleware.Business/        ← interfaces, DTOs, servicios de negocio
├── Booking.Middleware.DataManagement/  ← repositorios sobre clientes gRPC
└── Booking.Middleware.DataAccess/      ← stubs gRPC generados desde .proto
```

## Puertos

| Puerto | Protocolo | Uso |
|--------|-----------|-----|
| 5500   | HTTP      | Health check (`/health`) |
| 5500   | HTTP/2    | gRPC server (EventBus + Resolver) |

## Configuración

Copiar `appsettings.example.json` → `appsettings.Development.json` y completar:

```json
{
  "GrpcEndpoints": {
    "Auditoria": "http://localhost:5401",
    "Auth":      "http://localhost:5201",
    "Servicio":  "http://localhost:5202"
  }
}
```

En producción (Railway), configurar como variables de entorno:

```
GrpcEndpoints__Auditoria=https://<auditoria-url>
GrpcEndpoints__Auth=https://<auth-url>
GrpcEndpoints__Servicio=https://<servicio-url>
```

## Ejecución local

```bash
# Desde la raíz del repositorio
dotnet run --project src/Booking.Middleware.Api

# Health check
curl http://localhost:5500/health
```

## Protos

| Archivo               | Rol en este proyecto | Consumido por |
|-----------------------|----------------------|---------------|
| `eventos.proto`       | SERVER               | Cliente, Auth, Servicio |
| `audit.proto`         | CLIENT               | Auditoria |
| `auth.proto`          | CLIENT               | Auth |
| `servicio.proto`      | CLIENT               | Servicio |