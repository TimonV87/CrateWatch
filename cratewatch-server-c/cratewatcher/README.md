# cratewatcher (C# port of cratewatch-server)

ASP.NET Core 10 port of the Java/Micronaut backend in `../../cratewatch-server`. Same REST API under `/api/v1`,
same Postgres schema, same environment variables (`DB_*`, `JWT_GENERATOR_SIGNATURE_SECRET`, `THEAUDIODB_API_KEY`,
`COVER_ART_STORAGE_PATH`, `ARTIST_PHOTO_STORAGE_PATH`), listening on port 8080.

```bash
dotnet run          # needs Postgres 18 (uuidv7()) at localhost:5432
dotnet test    # integration tests start a Postgres 18 container: Docker must be running
docker build -t cratewatch-server:0.1 .   # same tag docker-compose.yml expects
```

Accounts are created on the command line (there is no registration): `dotnet run -- create-user <name>` locally, or see `DEPLOY.md` for the container.

Local secret (kept outside the repo, loaded only when `ASPNETCORE_ENVIRONMENT=Development`):

```bash
dotnet user-secrets set "JWT_GENERATOR_SIGNATURE_SECRET" "$(openssl rand -hex 32)"
```

In Production the secret must come from a real environment variable (docker-compose passes it from `.env`).

Layout is package-by-feature, mirroring `com.cratewatch.<feature>.*`:
`Artists`, `Releases`, `Users`, `MusicBrainz`, `CoverArt`, `TheAudioDb` (each with `Controllers/Domain/Repositories/Services` as needed),
plus `Db` (EF Core context + DbUp migrator) and `Configuration`.

| Java / Micronaut           | C# / ASP.NET Core                          |
|----------------------------|--------------------------------------------|
| Micronaut Data JDBC        | EF Core + Npgsql (snake_case columns)      |
| Flyway `db/migration`      | DbUp, embedded `Db/Migration/*.sql`        |
| Micronaut Security JWT     | JwtBearer + `/api/v1/login` (`TokenService`) |
| `@Client` interfaces       | typed `HttpClient`s                        |
| jBCrypt                    | BCrypt.Net-Next (hashes are compatible)    |

On first start against a database Flyway already created, V1 is recorded as applied instead of re-run.
