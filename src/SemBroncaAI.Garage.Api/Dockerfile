FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/SemBroncaAI.Garage.Domain/SemBroncaAI.Garage.Domain.csproj src/SemBroncaAI.Garage.Domain/
COPY src/SemBroncaAI.Garage.Application/SemBroncaAI.Garage.Application.csproj src/SemBroncaAI.Garage.Application/
COPY src/SemBroncaAI.Garage.DataProtection/SemBroncaAI.Garage.DataProtection.csproj src/SemBroncaAI.Garage.DataProtection/
COPY src/SemBroncaAI.Garage.Infrastructure/SemBroncaAI.Garage.Infrastructure.csproj src/SemBroncaAI.Garage.Infrastructure/
COPY src/SemBroncaAI.Garage.Api/SemBroncaAI.Garage.Api.csproj src/SemBroncaAI.Garage.Api/
RUN dotnet restore src/SemBroncaAI.Garage.Api/SemBroncaAI.Garage.Api.csproj

FROM restore AS build
COPY src/ src/
RUN dotnet publish src/SemBroncaAI.Garage.Api/SemBroncaAI.Garage.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false && \
    rm -f /app/publish/appsettings.Development.json

FROM build AS migrations
RUN dotnet tool install --global dotnet-ef --version 10.0.4
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/SemBroncaAI.Garage.Infrastructure", "--startup-project", "src/SemBroncaAI.Garage.Api", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG APP_VERSION=local
LABEL org.opencontainers.image.revision=$APP_VERSION
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* && \
    mkdir -p /var/lib/sbgarage/data-protection /var/lib/sbgarage/brand-assets /ms-playwright && \
    ./.playwright/node/linux-x64/node ./.playwright/package/cli.js install --with-deps chromium && \
    rm -rf /var/lib/apt/lists/* && \
    chown -R "$APP_UID:$APP_UID" /var/lib/sbgarage /ms-playwright /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SemBroncaAI.Garage.Api.dll"]
