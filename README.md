# cd-watcher

Personal CD collection tracker. Artists import their main albums/EPs from MusicBrainz; each release can be moved to a Wishlist or marked Owned.

See [CONTEXT.md](CONTEXT.md) for domain terms and [docs/adr/](docs/adr/) for design decisions.

## Run everything via Docker

The backend image is built via the Micronaut Gradle plugin, not a committed Dockerfile:

```bash
cd cd-watcher-server && ./gradlew dockerBuild && cd ..
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
cd cd-watcher-server
./gradlew run
```

**Frontend:**
```bash
cd cd-watcher-web
npm install
npm run dev
```

## Backend package convention

Package-by-feature, matching `investments-server`: `com.cdwatcher.<feature>.{controllers,domain,repositories,services}` (e.g. `com.cdwatcher.artist.controllers.ArtistController`). Entities use Micronaut Data JDBC (`@MappedEntity` + `@JdbcRepository`) with Lombok, not Hibernate JPA.
