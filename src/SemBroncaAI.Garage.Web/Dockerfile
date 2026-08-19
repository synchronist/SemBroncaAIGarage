FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY src/SemBroncaAI.Garage.Domain/SemBroncaAI.Garage.Domain.csproj src/SemBroncaAI.Garage.Domain/
COPY src/SemBroncaAI.Garage.Application/SemBroncaAI.Garage.Application.csproj src/SemBroncaAI.Garage.Application/
COPY src/SemBroncaAI.Garage.Web/SemBroncaAI.Garage.Web.csproj src/SemBroncaAI.Garage.Web/
RUN dotnet restore src/SemBroncaAI.Garage.Web/SemBroncaAI.Garage.Web.csproj

FROM restore AS build
COPY src/ src/
RUN dotnet publish src/SemBroncaAI.Garage.Web/SemBroncaAI.Garage.Web.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false && \
    rm -f /app/publish/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG APP_VERSION=local
LABEL org.opencontainers.image.revision=$APP_VERSION
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* && \
    mkdir -p /var/lib/sbgarage/data-protection && \
    chown -R "$APP_UID:$APP_UID" /var/lib/sbgarage /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SemBroncaAI.Garage.Web.dll"]
