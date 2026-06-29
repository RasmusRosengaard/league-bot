# LoL Match-Alert Discord Bot

[![CI/CD](https://github.com/RasmusRosengaard/league-bot/actions/workflows/ci.yml/badge.svg)](https://github.com/RasmusRosengaard/league-bot/actions/workflows/ci.yml)

En Discord-bot i C# / .NET 10 der lader brugere abonnere på League of Legends-konti.
Når en abonneret konto har spillet en ny kamp, poster botten automatisk et embed i
Discord-kanalen med resultatet: champion, win/loss, KDA, kø og kamplængde — med
champion-ikon fra Data Dragon.

Projektet er bygget som et lærings-/portfolio-projekt med eksplicit fokus på **DevOps
og CI/CD**: kodekvalitet, tests og en fungerende GitHub Actions-pipeline vægter lige
så højt som funktionaliteten.

---

## ⚠️ Vigtig begrænsning: ingen "live game"-funktion

Riot har **deaktiveret Spectator-V5-endpointet** for League of Legends for at forhindre
de-anonymisering af spillere. Det betyder at man **ikke** længere kan se hvem der spiller
*lige nu* (og som hvilken champion) — og det ville desuden være imod Riots udvikler-vilkår.

Denne bot er derfor en **post-match**-løsning: nye kampe opdages *efter* de er spillet,
via det fuldt understøttede **Match-V5**-endpoint. Botten bruger kun officielle,
dokumenterede Riot-endpoints og de-anonymiserer ikke spillere ud over hvad disse
endpoints frit eksponerer.

---

## Funktioner

Slash-kommandoer:

| Kommando | Beskrivelse |
|---|---|
| `/subscribe riotid:<gameName#tagLine> region:<region>` | Abonnér den aktuelle kanal på en konto |
| `/unsubscribe riotid:<gameName#tagLine>` | Fjern et abonnement i den aktuelle kanal |
| `/list` | Vis alle konti den aktuelle kanal abonnerer på |
| `/ping` | Health check — bekræfter at botten svarer |

Kommandoerne registreres **instant** i de servere botten er medlem af (og når den
joiner en ny). Sæt `Discord:TestGuildId` for at låse registreringen til én bestemt
guild; uden nogen guilds i cache registreres globalt (kan tage op til ~1 time).

Baggrundslogik:

- En `BackgroundService` poller på et konfigurerbart interval (default 3 minutter).
- Polling dedupes pr. konto: samme konto fulgt i flere kanaler giver **ét** API-kald,
  og nye kampe fan-out'es til alle kanaler der følger kontoen.
- Det sidst-sete match-id gemmes pr. PUUID, så **samme kamp aldrig postes to gange**
  (idempotens), heller ikke på tværs af kanaler.
- En fejl på én konto stopper ikke poll-løkken — den logges, og resten fortsætter.

---

## Arkitektur

```
src/
  Core/            Domæne + forretningslogik (ingen Discord/HTTP/DB-detaljer)
                   - Riot-modeller, IRiotClient-interface, region-routing
                   - MatchDiff (ren diff-logik), MatchPollingCoordinator
                   - Subscription/LastSeenMatch + repository-interfaces
  Infrastructure/  - RiotClient (HttpClient + resilience), IRiotHostResolver
                   - BotDbContext (EF Core + PostgreSQL), repositories, migrationer
  Bot/             - Discord.Net host, slash-kommandoer, poll-BackgroundService
                   - embeds, DI-opsætning, Program.cs
tests/
  Tests/           xUnit: unit-tests (netværksfri) + integrationstests
                   (WireMock.Net for Riot, Testcontainers/Postgres for DB)
```

`IRiotClient` ligger bag et interface, så Riot-kaldene kan mockes. Poll-/diff-logikken
er en ren funktion der kan unit-testes helt uden netværk.

### Teknologi

.NET 10 (LTS) · Discord.Net · EF Core + Npgsql (PostgreSQL) ·
`Microsoft.Extensions.Http.Resilience` (Polly v8) · `IHttpClientFactory` ·
xUnit · WireMock.Net · Testcontainers · Docker / docker-compose

### Riot API-flow (post-match)

1. Oversæt `gameName#tagLine` til en **PUUID** via `account-v1` (regional routing).
2. Hent seneste match-ids via **Match-V5** (`/matches/by-puuid/{puuid}/ids`).
3. Diff mod sidst-sete match-id → hvilke kampe er nye?
4. Hent detaljer (`/matches/{matchId}`), find deltageren med rette PUUID, byg embed.

Bemærk forskellen mellem **platform**-routing (euw1, na1, kr …) og **regional**-routing
(europe, americas, asia, sea). account-v1 og match-v5 bruger regional routing — se
`Core/Riot/Regions.cs` for mappingen.

---

## Kom i gang lokalt

### Forudsætninger

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (til Postgres og containerkørsel)

### 1. Hent en Riot-API-nøgle

1. Log ind på [developer.riotgames.com](https://developer.riotgames.com).
2. Kopiér din **development key** (udløber hver 24. time) — eller ansøg om en
   **personal/production key** til vedvarende drift.

### 2. Opret en Discord-bot

1. Gå til [discord.com/developers/applications](https://discord.com/developers/applications) → **New Application**.
2. Under **Bot** → kopiér **Token** (det er din `DISCORD_TOKEN`).
3. Under **Installation** / **OAuth2** → giv `bot` + `applications.commands` scopes og
   inviter botten til din server.
4. (Valgfrit) find din servers **Guild ID** (Developer Mode → højreklik server → Copy ID)
   for instant kommando-registrering under udvikling.

### 3a. Kør alt med docker-compose (nemmest)

```bash
cp .env.example .env      # udfyld RIOT_API_KEY og DISCORD_TOKEN
docker compose up --build
```

Dette starter Postgres + botten. Migrationer køres automatisk ved opstart.

### 3b. Kør botten direkte (med ekstern/lokal Postgres)

Brug **.NET user secrets** så hemmeligheder aldrig havner i kildekoden:

```bash
cd src/Bot
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=lolmatchalert;Username=postgres;Password=postgres"
dotnet user-secrets set "Riot:ApiKey" "RGAPI-..."
dotnet user-secrets set "Discord:Token" "din-token"
# valgfrit, instant kommandoer i én guild:
dotnet user-secrets set "Discord:TestGuildId" "123456789012345678"

dotnet run
```

### Migrationer som separat trin

Migrationer bruger EF Core (ikke `EnsureCreated`) og kan køres uafhængigt:

```bash
dotnet tool restore
ConnectionStrings__Default="Host=localhost;...;Password=postgres" \
  dotnet ef database update \
  --project src/Infrastructure/LolMatchAlert.Infrastructure.csproj \
  --startup-project src/Infrastructure/LolMatchAlert.Infrastructure.csproj
```

Sæt `Database:AutoMigrate=false` hvis du selv vil styre migrationer i drift.

---

## Konfiguration

Alle hemmeligheder læses fra konfiguration/miljøvariabler — **aldrig hardcodet**.
Miljøvariabler bruger `__` hvor .NET-konfiguration bruger `:` (f.eks.
`Riot__ApiKey` → `Riot:ApiKey`).

| Nøgle | Beskrivelse | Default |
|---|---|---|
| `ConnectionStrings:Default` | Postgres connection string | – (påkrævet) |
| `Riot:ApiKey` | Riot-API-nøgle | – (påkrævet) |
| `Riot:MatchIdsPerPoll` | Antal match-ids pr. poll | `5` |
| `Discord:Token` | Discord bot-token | – (påkrævet) |
| `Discord:TestGuildId` | Lås kommando-registrering til én guild (ellers alle bottens guilds) | `null` |
| `Polling:Interval` | Poll-interval (`hh:mm:ss`) | `00:03:00` |
| `Polling:DataDragonVersion` | Data Dragon-version til ikoner | se `appsettings.json` |
| `Database:AutoMigrate` | Kør migrationer ved opstart | `true` |

Se `.env.example` og `src/Bot/appsettings.example.json`.

---

## Tests

```bash
dotnet test
```

- **Unit-tests** (netværksfri): diff-/poll-logik og beskedformatering, med `IRiotClient` mocket.
- **Integrationstests** mod **WireMock.Net** der returnerer gemte eksempel-JSON-svar fra
  account-v1 og match-v5 (deterministisk, uden det rigtige Riot-API).
- **Postgres-integrationstest** via **Testcontainers** (lokalt) der verificerer at
  migrationer kører og at abonnement-/idempotens-logikken virker. I CI bruges i stedet en
  Postgres-**service-container** (sættes via `TEST_POSTGRES_CONNECTION`).

> Postgres-testene kræver at Docker kører.

Coverage genereres med `--collect:"XPlat Code Coverage"` (Cobertura).

---

## CI/CD-pipeline

`.github/workflows/ci.yml` kører på push og pull request mod `main`:

1. **Build** – `dotnet restore` → `dotnet build` (warnings-as-errors).
2. **Migrationer** – et separat trin kører EF-migrationer mod en Postgres-service-container.
3. **Test** – `dotnet test` mod samme service-container, med coverage.
4. **Coverage** – ReportGenerator skriver en procent i job-summary og uploader en rapport som artifact.
5. **Docker** – multi-stage `Dockerfile` bygges; image tagges med commit-SHA (og `latest` på `main`).
6. **Push** – image pushes til **GitHub Container Registry (ghcr.io)** — kun på `main`, ikke på pull requests (bruger den indbyggede `GITHUB_TOKEN`).
7. **Deploy** – et tydeligt markeret, gated placeholder-job (`vars.ENABLE_DEPLOY == 'true'`) der viser hvor en deploy til f.eks. Azure Container Apps / Railway / en VPS ville hænge.

### Forventede GitHub secrets

Build/test kræver **ingen** secrets (tests bruger fakes/containere). Til en rigtig deploy
forventes:

| Secret | Brug |
|---|---|
| `RIOT_API_KEY` | Riot personal/production-nøgle |
| `DISCORD_TOKEN` | Discord bot-token |

Push til ghcr bruger den indbyggede `GITHUB_TOKEN`.

---

## Hosting

Botten er en langtidskørende proces (udgående gateway-WebSocket via Discord.Net) og skal
køre 24/7 et sted. Den behøver **ingen** offentlig URL/indgående port. Naturlige mål:
en always-on maskine med `docker compose up`, eller image'et fra ghcr deployet til en
container-platform (Azure Container Apps, Railway, Fly.io, en VPS …).

Husk: en Riot **development-nøgle udløber hver 24. time** — brug en personal/production-nøgle
til drift.

---

## Drift & fejlfinding

### Persistens af data

Al state (abonnementer + sidst-sete kampe) ligger i Postgres — bot-containeren er stateless.
Postgres gemmer i det navngivne volume `pgdata`:

| Handling | Data |
|---|---|
| `docker compose stop` / `start` / `restart` | beholdes |
| `docker compose down` → `up` | beholdes (volumet `pgdata` overlever) |
| `docker compose down -v` | **slettes** (`-v` fjerner volumet) |

Ved deploy til en sky-platform skal du sikre en tilsvarende persistent database, ellers
mistes data ved hver redeploy.

### "Applikationen svarede ikke" på en slash-kommando

Discord kræver at en interaktion ack'es inden for **3 sekunder**. Kommandoer der laver
databasekald kalder derfor `DeferAsync()` med det samme og svarer via `FollowupAsync()`,
så et lidt tungt første DB-kald ikke rammer fristen. Tilføjer du nye kommandoer med
I/O, så følg samme mønster. Brug `/ping` til hurtigt at bekræfte at botten svarer.

### Kommandoer dukker ikke op

Guild-kommandoer er instant. Hvis du i stedet kører globalt (ingen guilds / ingen
`TestGuildId`), kan der gå op til ~1 time før de propagerer.

---

## Licens

Dette projekt er ikke tilknyttet Riot Games. League of Legends og Riot Games er
varemærker tilhørende Riot Games, Inc.
