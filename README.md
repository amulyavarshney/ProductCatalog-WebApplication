# Book Catalog

CQRS book catalog — ASP.NET Core 6 microservices with a live demo UI.

**Demo:** [https://amulyavarshney.github.io/book-catalog/](https://amulyavarshney.github.io/book-catalog/)

**Repo:** [github.com/amulyavarshney/book-catalog](https://github.com/amulyavarshney/book-catalog)

## What you get

| Piece | Role |
|-------|------|
| **Demo UI** (`docs/`) | Browse, search, filter, sort, paginate, create, update, soft-delete |
| **BookCommand.Service** | Write API + transactional outbox |
| **BookQuery.Service** | Read API + RabbitMQ consumer (idempotent soft-delete projection) |
| **BookCatalog.Gateway** | YARP reverse proxy (`:8080`) |
| **BookCatalog.Contracts** | Shared DTOs, events, exceptions |
| **BookCatalog.Tests** | Unit + API contract tests |
| **docker-compose** | SQL Server, RabbitMQ, both services, gateway |

Architecture:

```
Client / Demo UI
    → Gateway (:8080)
         ├─ /command/**  → BookCommand (:5001) → BookCommand DB → Outbox → RabbitMQ
         └─ /query/**    → BookQuery (:5002)  ← RabbitMQ → BookQuery DB
```

## Demo UI

Open the GitHub Pages site, or open `docs/index.html` locally.

- **Demo (local)** — full CRUD in browser storage (works offline on Pages), including soft-delete
- **Live API** — talk to a running gateway (`http://localhost:8080` by default)
- **Reset demo** — restore seed books in demo mode

Features mirrored from the API:

- List with `search`, `author`, `sortBy`, `sortDir`, `page`, `pageSize`
- Create / update (including **title**)
- Soft-delete on both sides of the services; demo mode marks books deleted locally

## Quick start (Docker)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Demo UI (Pages) | https://amulyavarshney.github.io/book-catalog/ |
| Gateway | http://localhost:8080 |
| Command API | http://localhost:5001 |
| Query API | http://localhost:5002 |
| RabbitMQ UI | http://localhost:15672 (`guest` / `guest`) |

Gateway examples:

```bash
curl -X POST http://localhost:8080/command/api/v1/book \
  -H "Content-Type: application/json" \
  -d '{"title":"Clean Code","description":"A handbook","author":"Robert C. Martin"}'

curl "http://localhost:8080/query/api/v1/book?page=1&pageSize=20&search=clean&sortBy=title"
```

Health: `GET /health` on each service and the gateway.

## Local run (without Docker)

1. Start SQL Server and RabbitMQ.
2. Set `ConnectionStrings:DefaultConnection` and `RabbitMQConfig` in each service `appsettings.json`.
3. Run:

```bash
dotnet run --project BookCommand.Service
dotnet run --project BookQuery.Service
dotnet run --project BookCatalog.Gateway
```

EF migrations create schema on startup (and create the database if missing).

## API

### Command — `/api/v1/book`

| Method | Path | Status |
|--------|------|--------|
| POST | `/api/v1/book` | 201 |
| PUT | `/api/v1/book/{id}` | 204 |
| DELETE | `/api/v1/book/{id}` | 204 |

### Query — `/api/v1/book`

| Method | Path | Status |
|--------|------|--------|
| GET | `/api/v1/book` | 200 (paged) |
| GET | `/api/v1/book/{id}` | 200 / 404 |

Swagger is enabled in Development at `/swagger`.

## Authentication

JWT is optional (`Authentication:Enabled`, default `false`). When enabled, a global authorize filter protects controllers.

## Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `RabbitMQConfig:*` | Host, credentials, exchange, queue, routing key, DLQ |
| `Authentication:Enabled` | Toggle JWT |

## Tests

```bash
dotnet test BookCatalog.Tests
```

## GitHub Pages

The `docs/` folder is published via `.github/workflows/deploy-pages.yml` to the project site at `/book-catalog/`.

## License

See [LICENSE.txt](LICENSE.txt).
