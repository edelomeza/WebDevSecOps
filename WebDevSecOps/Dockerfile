FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar todos los .csproj (layer caching para restore)
COPY WebDevSecOps/WebDevSecOps.csproj WebDevSecOps/
COPY tests/WebDevSecOps.UnitTests/WebDevSecOps.UnitTests.csproj tests/WebDevSecOps.UnitTests/
COPY tests/WebDevSecOps.IntegrationTests/WebDevSecOps.IntegrationTests.csproj tests/WebDevSecOps.IntegrationTests/
COPY tests/WebDevSecOps.E2E/WebDevSecOps.E2E.csproj tests/WebDevSecOps.E2E/
COPY tests/WebDevSecOps.SecurityTests/WebDevSecOps.SecurityTests.csproj tests/WebDevSecOps.SecurityTests/

RUN dotnet restore WebDevSecOps/WebDevSecOps.csproj

COPY . .

RUN dotnet publish WebDevSecOps/WebDevSecOps.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 443
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "WebDevSecOps.dll"]
