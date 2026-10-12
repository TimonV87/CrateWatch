# CrateWatch

Personal physical-media collection tracker. Search artists, import their albums/EPs from MusicBrainz, and move releases to a Wishlist or mark them Owned. Started with CDs; vinyl LPs and per-store availability watching (via `ShopLink`, see [CONTEXT.md](CONTEXT.md)) are planned next.

See [CONTEXT.md](CONTEXT.md) for domain terms and [docs/adr/](docs/adr/) for design decisions.

## Run everything via Docker

The backend image is built via the Micronaut Gradle plugin, not a committed Dockerfile:

```bash
cd cratewatch-server && ./gradlew dockerBuild && cd ..
cp .env.example .env
docker-compose up
```

- Backend: http://localhost:8080
- Frontend: http://localhost:5173
- Postgres: localhost:5432

## Run locally without Docker

Requires a Postgres instance reachable at `localhost:5432` (or set `DB_HOST`/`DB_PORT`/`DB_NAME`/`DB_USER`/`DB_PASSWORD` env vars).

**Backend:**
```bash
cd cratewatch-server
./gradlew run
```

**Frontend:**
```bash
cd cratewatch-web
npm install
npm run dev
```

## Backend package convention

Package-by-feature, matching `investments-server`: `com.cratewatch.<feature>.{controllers,domain,repositories,services}` (e.g. `com.cratewatch.artist.controllers.ArtistController`). Entities use Micronaut Data JDBC (`@MappedEntity` + `@JdbcRepository`) with Lombok, not Hibernate JPA.
