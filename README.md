# AspNetCore6.Microservices.BookCatalog

CQRS book catalog built with ASP.NET Core 6: separate write/read microservices, transactional outbox, RabbitMQ projection, YARP gateway, and Docker Compose for local runs.

## Architecture

```
Client → Gateway (:8080)
           ├─ /command/**  → BookCommand.Service (:5001) → BookCommand DB
           │                      └─ Outbox → RabbitMQ
           └─ /query/**    → BookQuery.Service (:5002)  → BookQuery DB
                                  └─ Consumer ← RabbitMQ
```

- **BookCommand.Service** — create/update/soft-delete; persists outbox rows in the same DB transaction, then a background publisher sends events to RabbitMQ.
- **BookQuery.Service** — paged reads; consumer applies events idempotently (upsert + soft-delete) into the read model.
- **BookCatalog.Contracts** — shared DTOs, events, and exceptions.
- **BookCatalog.Gateway** — YARP reverse proxy on port `8080`.

Soft-delete is used on **both** sides (`IsDeleted` + EF query filters).

## Technologies

- ASP.NET Core 6 / EF Core 7 / SQL Server / RabbitMQ / YARP / Swagger / xUnit

## Quick start (Docker)

```bash
docker compose up --build
```

Services:

| Service | URL |
|---------|-----|
| Gateway | http://localhost:8080 |
| Command API | http://localhost:5001 |
| Query API | http://localhost:5002 |
| RabbitMQ management | http://localhost:15672 (guest/guest) |

Examples via gateway:

```bash
# Create
curl -X POST http://localhost:8080/command/api/v1/book \
  -H "Content-Type: application/json" \
  -d '{"title":"Clean Code","description":"A handbook","author":"Robert C. Martin"}'

# List (paged)
curl "http://localhost:8080/query/api/v1/book?page=1&pageSize=20&search=clean&sortBy=title&sortDir=asc"

# Get by id
curl http://localhost:8080/query/api/v1/book/1
```

Health: `GET /health` on each service (and the gateway).

## Local run (without Docker)

1. Start SQL Server and RabbitMQ.
2. Create databases `BookCommand` and `BookQuery` (or let EF migrations create schema after DBs exist).
3. Update `ConnectionStrings:DefaultConnection` and `RabbitMQConfig` in each service `appsettings.json`.
4. Run:

```bash
dotnet run --project BookCommand.Service
dotnet run --project BookQuery.Service
dotnet run --project BookCatalog.Gateway
```

Migrations apply automatically on startup when using a relational database.

## API

### Command (`/api/v1/book`)

| Method | Path | Status | Notes |
|--------|------|--------|-------|
| POST | `/api/v1/book` | 201 | Body: `title` (required, max 50), `description`, `author` |
| PUT | `/api/v1/book/{id}` | 204 | Updates title, description, author |
| DELETE | `/api/v1/book/{id}` | 204 | Soft-delete |

### Query (`/api/v1/book`)

| Method | Path | Status | Notes |
|--------|------|--------|-------|
| GET | `/api/v1/book` | 200 | Query: `page`, `pageSize` (max 100), `search`, `author`, `sortBy`, `sortDir` |
| GET | `/api/v1/book/{id}` | 200/404 | Single book |

Swagger is available in Development at `/swagger` on each service.

## Authentication (optional)

JWT bearer is wired but **disabled by default** (`Authentication:Enabled: false`). When enabled, set:

```json
"Authentication": {
  "Enabled": true,
  "Jwt": {
    "Issuer": "BookCatalog",
    "Audience": "BookCatalog",
    "Key": "a-long-secret-key-at-least-32-chars"
  }
}
```

Controllers use a global `[Authorize]` filter when `Authentication:Enabled` is `true`. With auth disabled (default), endpoints remain open for local demos.

## Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `RabbitMQConfig:*` | Host, credentials, exchange, queue, routing key, DLQ |
| `Authentication:Enabled` | Toggle JWT |

## Tests

```bash
dotnet test BookCatalog.Tests
```

Covers command outbox writes, idempotent query projections (including soft-delete), and HTTP contracts (201/400/404/pagination).

## Project layout

```
BookCatalog.Contracts/     Shared DTOs and events
BookCommand.Service/       Write side + outbox publisher
BookQuery.Service/         Read side + RabbitMQ consumer
BookCatalog.Gateway/       YARP gateway
BookCatalog.Tests/         Unit + API tests
docker-compose.yml
```
