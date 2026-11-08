# API ASP.NET Core : image multi-stage.
# Étape 1 (sdk) compile et publie, étape 2 (aspnet) ne contient que le runtime et l'application.
# Construire depuis le dossier backend/ :  docker build -t coworking-backend backend

# ---------- Étape 1 : build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# On copie d'abord le .csproj seul : tant qu'il ne change pas,
# Docker réutilise la couche du "restore" (téléchargement des paquets NuGet) en cache.
COPY src/CoworkingBooking.Api/CoworkingBooking.Api.csproj src/CoworkingBooking.Api/
RUN dotnet restore src/CoworkingBooking.Api/CoworkingBooking.Api.csproj

# Puis le code source, et la publication en Release.
COPY src/ src/
RUN dotnet publish src/CoworkingBooking.Api/CoworkingBooking.Api.csproj \
    -c Release -o /app --no-restore

# ---------- Étape 2 : runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl sert uniquement au healthcheck (docker compose, puis Kubernetes utilisera ses propres sondes).
# tzdata fournit le fuseau Africa/Casablanca utilisé par Services/LocalTime.cs.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

# L'image aspnet fournit un utilisateur non-root "app" : l'API ne tourne jamais en root.
USER $APP_UID

# Les images .NET 8+ écoutent sur le port 8080 par défaut (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=5 \
    CMD curl -fs http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CoworkingBooking.Api.dll"]
