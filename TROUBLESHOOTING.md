# Troubleshooting local setup

Backend + frontend run on the host. Only Postgres runs in Docker.

## Port 5433, not 5432

Postgres listens on 5432 inside the container. We publish it on the host as 5433 because 5432 is
usually already taken on Windows by an older Postgres install or another project.

In `docker-compose.yml` the mapping is `"${POSTGRES_PORT}:5432"` - left side is your machine,
right side is inside the container. Only the left side changes.

So:

- DBeaver, pgAdmin, the API on your machine -> `localhost:5433`
- `docker compose exec postgres psql ...` -> runs inside the container, still 5432, nothing to change

Check what owns 5432 if you are curious:

```
netstat -ano | findstr :5432
```

Don't fight it. Leave it alone and use 5433.

If you still want a different port, change `POSTGRES_PORT` in your `.env`, then
`docker compose up -d` again. Never edit the connection string in code - it is built from `.env`
in `Program.cs`.

## "Container is up but connection refused"

`Up` is not the same as ready. Postgres needs a few seconds to initialise, longer on first run
because it has to create the data directory.

```
docker compose ps
```

Wait for `Up ... (healthy)`. If it stays `Up` without `(healthy)`, look at the logs:

```
docker compose logs postgres
```

Other things that cause this:

- Docker Desktop is not running. The error mentions `dockerDesktopLinuxEngine ... cannot find the file specified`. Start Docker Desktop and wait for the whale icon to settle.
- Your `.env` is missing. Compose will warn about empty variables and the port mapping breaks. Copy `.env.example` to `.env`.
- `POSTGRES_PORT` in `.env` does not match what the container published. Confirm with `docker compose ps`, the PORTS column shows `0.0.0.0:5433->5432/tcp`.
- Something else grabbed 5433 too. Pick another port in `.env` and restart.

## Wipe the database and start fresh

The data lives in a named volume, so `docker compose down` alone keeps it. To actually delete it:

```
docker compose down -v
docker compose up -d
cd backend/MiniPos.Api
dotnet ef database update
```

`-v` removes the `minipos-data` volume. Everything in the database is gone, no confirmation.

`dotnet ef database update` rebuilds the schema. The test user is seeded automatically the next
time you run the API in Development.

## Other things that bite

`dotnet ef` not found, or it errors about version mismatch:

```
dotnet tool update --global dotnet-ef --version "10.*"
```

The project is EF Core 10. An older global tool, for example 8.x, fails on every migration command.

`dotnet build` fails with "the file is locked by MiniPos.Api":

The API is still running from another terminal. Ctrl+C it, or:

```
taskkill /F /IM MiniPos.Api.exe
```

`POSTGRES_HOST is not set. Copy .env.example to .env in the repo root.`

Exactly what it says. The API reads `.env` from the repo root, not from `backend/MiniPos.Api`.

## Test login

`admin` / `admin123`. Seeded in Development only, and only when the Users table is empty.
