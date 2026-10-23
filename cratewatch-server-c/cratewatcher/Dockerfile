FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY cratewatcher.csproj .
RUN dotnet restore cratewatcher.csproj
COPY . .
RUN dotnet publish cratewatcher.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
# Run as the image's non-root "app" user; the cache folders must exist and be owned by it before the volumes mount.
RUN mkdir -p /app/cover-art-cache /app/artist-photo-cache && chown -R $APP_UID /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "cratewatcher.dll"]
