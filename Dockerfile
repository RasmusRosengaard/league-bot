# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# global.json/Directory.Build.props/.editorconfig påvirker restore + build (analyzers).
COPY global.json Directory.Build.props .editorconfig ./

# Kopiér csproj'er først, så 'restore' kan caches uafhængigt af kildekode-ændringer.
COPY src/Core/LolMatchAlert.Core.csproj src/Core/
COPY src/Infrastructure/LolMatchAlert.Infrastructure.csproj src/Infrastructure/
COPY src/Bot/LolMatchAlert.Bot.csproj src/Bot/
RUN dotnet restore src/Bot/LolMatchAlert.Bot.csproj

# Resten af kildekoden + publish.
COPY src/ src/
RUN dotnet publish src/Bot/LolMatchAlert.Bot.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# libgssapi-krb5-2: Npgsql forsøger at loade GSSAPI/Kerberos ved opstart. Uden den
# logges en (harmløs, men støjende) fejl ved første DB-kald.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Kør som non-root.
RUN useradd --uid 1001 --create-home --shell /usr/sbin/nologin appuser
COPY --from=build /app ./
USER appuser

ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "LolMatchAlert.Bot.dll"]
