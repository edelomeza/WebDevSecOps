FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["WebDevSecOps.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 443
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "WebDevSecOps.dll"]
