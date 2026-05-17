FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Booking.Middleware.slnx", "./"]
COPY ["src/Booking.Middleware.Api/Booking.Middleware.Api.csproj", "src/Booking.Middleware.Api/"]
COPY ["src/Booking.Middleware.Business/Booking.Middleware.Business.csproj", "src/Booking.Middleware.Business/"]
COPY ["src/Booking.Middleware.DataAccess/Booking.Middleware.DataAccess.csproj", "src/Booking.Middleware.DataAccess/"]
COPY ["src/Booking.Middleware.DataManagement/Booking.Middleware.DataManagement.csproj", "src/Booking.Middleware.DataManagement/"]

RUN dotnet restore "Booking.Middleware.slnx"

COPY . .
RUN dotnet publish "src/Booking.Middleware.Api/Booking.Middleware.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Booking.Middleware.Api.dll"]