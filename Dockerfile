# ─── build stage ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore (cached separately from source for faster rebuilds)
COPY global.json ./
COPY src/GBA.Common/GBA.Common.csproj      src/GBA.Common/
COPY src/GBA.Data/GBA.Data.csproj          src/GBA.Data/
COPY src/GBA.Domain/GBA.Domain.csproj      src/GBA.Domain/
COPY src/GBA.Search/GBA.Search.csproj      src/GBA.Search/
COPY src/GBA.Services/GBA.Services.csproj  src/GBA.Services/
COPY src/GBA.Ecommerce/GBA.Ecommerce.csproj src/GBA.Ecommerce/
RUN dotnet restore src/GBA.Ecommerce/GBA.Ecommerce.csproj

# build + publish (R2R disabled: marginal for a long-running server, keeps build RID-free)
COPY src/ src/
RUN dotnet publish src/GBA.Ecommerce/GBA.Ecommerce.csproj \
        -c Release -p:PublishReadyToRun=false \
        -o /app/publish --no-restore

# ─── runtime stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./
RUN mkdir -p /app/logs /app/Data /app/Logs /app/Images \
    && chown -R $APP_UID:$APP_UID /app

# app binds Kestrel to 0.0.0.0:62506 (see Program.cs)
EXPOSE 62506
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

USER $APP_UID

ENTRYPOINT ["dotnet", "GBA.Ecommerce.dll"]
