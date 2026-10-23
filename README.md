# CrateWatch

Personal physical-media collection tracker. Search artists, import their albums from MusicBrainz, and move releases to a Collection or Wishlist. Started with CDs; vinyl LPs and per-store availability watching (via `ShopLink`, see [CONTEXT.md](CONTEXT.md)) are planned next.

See [CONTEXT.md](CONTEXT.md) for domain terms and [docs/adr/](docs/adr/) for design decisions. To host it, see [DEPLOY.md](DEPLOY.md).

## Run locally

Needs Postgres 18 (for `uuidv7()`). `docker compose up -d` starts one on `localhost:5432` (override with `DB_HOST`/`DB_PORT`/`DB_NAME`/`DB_USER`/`DB_PASSWORD`).

Two backends implement the same API on port 8080; run one at a time. The **C# backend is the one production runs and has all current features**. The Java backend is kept for testing and comparison (see "Java backend" below for what it lacks).

**Backend** (C#, ASP.NET Core 10) at http://localhost:8080:
```bash
cd cratewatch-server-c/cratewatcher
dotnet run -- create-user <name>   # accounts are created on the command line, there is no registration
dotnet run
dotnet test    # integration tests start a Postgres 18 container: Docker must be running
```

**Frontend** (Vue 3) at http://localhost:5173:
```bash
cd cratewatch-web
npm install
npm run dev
```

The backend README (`cratewatch-server-c/cratewatcher/README.md`) explains the JWT secret and project layout.

## Java backend (Micronaut)

```bash
cd cratewatch-server
./gradlew run        # port 8080, same database and environment variables as the C# backend
```

It runs the same Flyway-style migration on the same schema, so the two backends can share a database (the C# one recognises a Flyway-created schema and adopts it). Compared with the C# backend it does **not** yet have: the Collection/Wishlist endpoints (`PUT /releases/{id}/state`, `GET /releases?state=`), the per-type "switch" import (`?types=`), the `create-user` command, login rate limiting, or the 7-day token, and it still has the open `POST /auth/register`. So the Collection/Wishlist buttons and pages only work against the C# backend. See `cratewatch-server-c/cratewatcher/ISSUES.md` for the full list.
