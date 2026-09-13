# AGENTS.md

Working rules for anyone, human or agent, writing code in this repo.

## What this is

A scaled down POS used to train the IZI Soft dev team: products, sales, customers, settings,
izi-pay payments, AI-assisted sale entry. Not for production use.

## Stack

- Backend: ASP.NET Core 10 Web API with controllers, EF Core 10, Npgsql, JWT bearer auth, Swagger.
- Frontend: Next.js 14 App Router, TypeScript, plain CSS. No Tailwind, no UI library, no state library.
- AI service: FastAPI. Health endpoint only. Phase 2, stays empty until then.
- Database: PostgreSQL 16 in Docker.

Backend and frontend run on the host: `dotnet run` in `backend/MiniPos.Api`, `npm run dev` in
`frontend`. Only Postgres runs in Docker.

## Structure

```
backend/MiniPos.Api/    Program.cs, Models/Entities.cs, Data/AppDbContext.cs, Dtos/, Controllers/
frontend/               app/ (App Router pages), components/, lib/api.ts
ai-service/             main.py, requirements.txt
docs/                   API.md, PLAN.md
```

## Coding style

Simple over clever, ship over ceremony. Same style as IZI-Shop POS.

- Single API project. No Clean Architecture, no separate Application/Domain/Infrastructure projects.
- No repository pattern, no unit of work, no interface for a class with one implementation.
- No MediatR, no AutoMapper, no FluentValidation, no CQRS.
- Controllers talk to `AppDbContext` directly. A service class only when two controllers need the
  same logic.
- Small explicit DTO classes for request and response. Never return entities from controllers.
- Money is `numeric(14,3)` in the database and `decimal` in C#.
- Validation is plain `if` checks returning `BadRequest(new { error = "..." })`.
- Names in English, clear and boring: `ProductsController`, `CreateSaleRequest`, `SaleResponse`.
- Keep the file count low. Related small classes share a file.

## Comments

Almost none. Write code that reads by itself. A comment is allowed only for a business rule the
code cannot show, such as why stock is locked before it is decremented. No comments that repeat
the code, no banner comments, no section dividers, no emoji, no `TODO: implement later`, no
docstring blocks on obvious methods. If a comment could be deleted without losing information,
delete it.

## docs/API.md is frozen

`docs/API.md` is the source of truth for every route, field name, and type. Code matches it
exactly. It changes only with the repo owner's approval. If something in it is unclear, ask;
do not decide alone. Do not edit files in `docs/`.

## Git workflow

The repo owner handles git. Agents do not run any git command: no `init`, `add`, `commit`,
`push`, or branch changes. Report what changed and let the owner commit.

Humans: branch off `main`, small commits, English messages in the imperative, open a PR. CI must
be green before merge.
