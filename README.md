# Masroof — Private AI Expense Ledger (backend)

A production-grade, multi-user, fully on-prem expense ledger. Paste a bank SMS (Arabic/English),
a wallet notification, or a note like *"paid the plumber 150"* and a **local LLM** turns it into a
structured ledger row. Corrections become **rules** that are injected as hints next time — no
retraining. Questions are answered from **safe, user-scoped tool calls**, never text-to-SQL.

Built from `masroof-build-guide.md`. This repo contains the **.NET backend** and the **Angular 22
frontend** (`src/masroof-web`).

## Solution layout

```
src/
  Masroof.Domain/          entities, enums, taxonomy, text normalization
  Masroof.Application/     use cases (Parse, Correct, GetLedger, Reports, Ask, Rules), abstractions
  Masroof.Infrastructure/  EF Core + Dapper, Ollama LLM/embeddings, rules engine, pre-parser
  Masroof.Api/             ASP.NET Core minimal APIs, auth, ProblemDetails, health, rate limiting
  Masroof.Worker/          outbox processor: rule embeddings, batch/pending reparse
  masroof-web/             Angular 22 SPA (standalone, signals, new control flow)
deploy/
  docker/                  api + worker + web Dockerfiles, nginx.conf
  compose/                 local stack (SQL Server 2025, Ollama, Keycloak) + realm
```

### Frontend (`src/masroof-web`)
Angular 22 standalone app: feature screens for **Capture, Ledger, Insights, Ask, Learned rules**,
a typed `ApiService`, functional HTTP interceptors (auth + error→toast), signal-based state, a
bilingual EN/AR toggle with full RTL (logical CSS properties), light/dark theming via CSS custom
properties, ECharts (lazy-loaded) for insights, and skeleton loaders. The UI follows a
**"design-engineered" visual language** (Bklit-inspired): near-black canvas, blueprint grid with
crosshair markers, hairline borders, sharp corners, monospace uppercase micro-labels, and a heavy
grotesque display type — self-hosted **Geist / Geist Mono** (via `@fontsource`, no external font
CDN, consistent with zero-egress). **Auth** uses
`angular-auth-oidc-client` (Keycloak PKCE) wired in `core/auth`: a route guard redirects
unauthenticated users to Keycloak and the library attaches tokens to `/api`. It is gated by
`environment.auth.enabled` — off in development (the API's `Auth:DevBypass` means no token is
needed), on in production. Sign-in/out appears in the header only when enabled.

### Request pipeline (parse)
normalize → dedupe (SHA-256) → regex **pre-parser** → **rules** (exact → fuzzy → vector + lexical
gate) → **LLM** (JSON-schema-constrained) → merge + rule post-override → FluentValidation → persist
row + `ParseTrace`. If the LLM is down, a pre-parsed row is saved and an outbox job reparses later —
the user never loses input.

## Key design decisions

- **Clean architecture**: `Application` defines `IAppDbContext` and service abstractions; `Infrastructure` implements them. No MediatR — handlers are plain injected classes.
- **Row ownership**: an EF global query filter scopes `Transactions`/`Accounts`/`MerchantRules` to the authenticated user. Reporting uses Dapper, also user-scoped.
- **Embeddings**: `MerchantRules.Embedding` is SQL Server 2025's native `VECTOR(1024)`. EF does not map it (`[NotMapped]`); it is created by raw SQL in the migration and read/written by the rules engine via `VECTOR_DISTANCE`. `EmbeddingModel` tracks whether a rule has been embedded.
- **Ask**: `Microsoft.Extensions.AI` tool calling over `IReportQueries`; the `UserId` is always taken from the auth context, never from LLM arguments. Returns the tools it used for auditability.
- **LLM client**: `IChatClient` (OllamaSharp) + typed `HttpClient`s with Polly resilience (timeout, 1 retry, circuit breaker) and a concurrency limiter.
- **Auth**: Keycloak OIDC + JWT bearer in production; a config-gated **Dev bypass** (`Auth:DevBypass`) signs a fixed dev user in for local work.

## Run locally

### Full stack (Docker)
```bash
cd deploy/compose
cp .env.example .env            # set a strong SA_PASSWORD
docker compose up -d
docker compose exec ollama ollama pull qwen3:8b
docker compose exec ollama ollama pull bge-m3
```
API: http://localhost:8080 · Keycloak: http://localhost:8081 (admin/admin) · demo user: `demo`/`demo`.

### API only (dev, with Dev auth bypass)
Requires a reachable SQL Server (connection string in `appsettings.json`) and Ollama for parse/ask.
```bash
dotnet ef database update --project src/Masroof.Infrastructure --startup-project src/Masroof.Api
dotnet run --project src/Masroof.Api        # Development => DevBypass + auto-migrate
```

### Frontend (dev)
```bash
cd src/masroof-web
npm install        # first time
npm start          # ng serve on http://localhost:4200, proxies /api -> :8080
npm run build      # production bundle in dist/masroof-web/browser
```

## API

| Method | Route |
|---|---|
| POST | `/api/transactions/parse` |
| POST | `/api/transactions/parse-batch` |
| GET | `/api/transactions?month=&category=&direction=&q=&page=&pageSize=&needsReview=` |
| PATCH | `/api/transactions/{id}` |
| DELETE | `/api/transactions/{id}` |
| GET | `/api/reports/summary?month=` · `/api/reports/trend?months=` |
| POST | `/api/ask` |
| GET | `/api/rules` · DELETE `/api/rules/{id}` |
| GET | `/api/categories` |
| GET | `/health/live` · `/health/ready` |

Errors use RFC 9457 ProblemDetails: `409` duplicate (+`existingTransactionId`), `422` unparseable, `502` LLM down.

## Build

```bash
dotnet build          # whole solution
```
