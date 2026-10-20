# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/JobRadar.API/JobRadar.API.csproj", "src/JobRadar.API/"]
COPY ["src/JobRadar.Application/JobRadar.Application.csproj", "src/JobRadar.Application/"]
COPY ["src/JobRadar.Domain/JobRadar.Domain.csproj", "src/JobRadar.Domain/"]
COPY ["src/JobRadar.Infrastructure/JobRadar.Infrastructure.csproj", "src/JobRadar.Infrastructure/"]
RUN dotnet restore "src/JobRadar.API/JobRadar.API.csproj"
COPY . .
RUN dotnet publish "src/JobRadar.API/JobRadar.API.csproj" -c Release --no-restore -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "JobRadar.API.dll"]
