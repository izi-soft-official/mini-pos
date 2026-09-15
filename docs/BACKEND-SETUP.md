# Backend setup

Quick notes so you can run the API. `docs/API.md` is the real contract.

Postgres runs in Docker on port 5433. The API and the frontend run on your machine, 5080 and 3000.

## First time

From the repo root:

    Copy-Item .env.example .env
    docker compose up -d
    docker compose ps

Wait until the status says `(healthy)`.

Then from `backend/MiniPos.Api`:

    dotnet tool install --global dotnet-ef --version 10.0.*
    dotnet ef database update
    dotnet run

Check `http://localhost:5080/api/health` returns ok. Swagger is at `/swagger`. Demo data
seeds itself on first run.

## Every day

Two terminals: `dotnet run` in `backend/MiniPos.Api`, `npm run dev` in `frontend`.

## Auth

`admin` / `admin123`. `POST /api/auth/login` gives you a token, send it as
`Authorization: Bearer <token>` on everything else. `GET /api/auth/me` restores the session.

Put `NEXT_PUBLIC_API_URL=http://localhost:5080` in `frontend/.env.local`.

## Works now

auth, categories, products, customers, sales, returns, health.

## Throws 500, don't call

`/api/settings`, `/api/users`, `/api/dashboard/*`. Hardcode `en` and `light` instead of
reading settings.

## Gotchas

- CORS errors: check `Cors:AllowedOrigins` in `appsettings.json` has `http://localhost:3000`.
- Empty 401: the header is malformed. `Bearer <token>`, one space, no quotes.
- `pageSize` caps at 100.
- Sale errors come back as `{ "error": "..." }` with a message you can show the cashier.
- No tax anywhere. Money is decimal, 3 places.

SQL: `docker compose exec postgres psql -U minipos -d minipos`, then type at the prompt. Exit
with `\q`. Don't use `-c "..."` from PowerShell, the quoting breaks.
