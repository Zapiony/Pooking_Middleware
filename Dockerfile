FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5500

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias (mejor caché de capas)
COPY ["src/Booking.Middleware.Api/Booking.Middleware.Api.csproj",             "src/Booking.Middleware.Api/"]
COPY ["src/Booking.Middleware.Business/Booking.Middleware.Business.csproj",   "src/Booking.Middleware.Business/"]
COPY ["src/Booking.Middleware.DataManagement/Booking.Middleware.DataManagement.csproj", "src/Booking.Middleware.DataManagement/"]
COPY ["src/Booking.Middleware.DataAccess/Booking.Middleware.DataAccess.csproj", "src/Booking.Middleware.DataAccess/"]

RUN dotnet restore "src/Booking.Middleware.Api/Booking.Middleware.Api.csproj"

# Copiar todo el código fuente y compilar
COPY . .
WORKDIR "/src/src/Booking.Middleware.Api"
RUN dotnet build "Booking.Middleware.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Middleware.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Booking.Middleware.Api.dll"]
