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

everything in API.md: auth, categories, products, customers, sales, returns, settings, users,
dashboard, health. AI stuff is phase 2.

## Settings

`GET /api/settings` works for any logged in user. read language and theme from it at startup,
drop the hardcoded `en`/`light`. only admin can `PUT`.

## Users and dashboard

- `/api/users` is admin only, others get 403.
- can't delete or demote the last active admin, 400. can't delete a user with sales either,
  deactivate them.
- `/api/dashboard/*` is admin + manager.
- no `from`/`to` means today. days are UTC, not Algeria time.
- `sales-by-day` returns every day in the range, empty days come back with 0.
- dashboard numbers are after returns.

## Gotchas

- CORS errors: check `Cors:AllowedOrigins` in `appsettings.json` has `http://localhost:3000`.
- Empty 401: the header is malformed. `Bearer <token>`, one space, no quotes.
- `pageSize` caps at 100.
- Errors come back as `{ "error": "..." }` with a message you can show the user.
- No tax anywhere. Money is decimal, 3 places.

SQL: `docker compose exec postgres psql -U minipos -d minipos`, then type at the prompt. Exit
with `\q`. Don't use `-c "..."` from PowerShell, the quoting breaks.
