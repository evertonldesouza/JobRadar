FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JobRadar.API/JobRadar.API.csproj", "src/JobRadar.API/"]
COPY ["src/JobRadar.Application/JobRadar.Application.csproj", "src/JobRadar.Application/"]
COPY ["src/JobRadar.Domain/JobRadar.Domain.csproj", "src/JobRadar.Domain/"]
COPY ["src/JobRadar.Infrastructure/JobRadar.Infrastructure.csproj", "src/JobRadar.Infrastructure/"]
RUN dotnet restore "src/JobRadar.API/JobRadar.API.csproj"
COPY . .
RUN dotnet publish "src/JobRadar.API/JobRadar.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JobRadar.API.dll"]