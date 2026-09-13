# Plan

How mini-pos gets built, in the order it gets built. `docs/API.md` says what the routes are;
this file says when they get filled in and by whom.

## How the training works

The repo ships as scaffolding: every route exists with the right verb, path, DTOs, and role
attributes, and every method body is `throw new NotImplementedException();`. A trainee picks one
task below, implements the bodies, wires the matching page, and opens a PR. The contract does not
move, so two people can work on different tasks without stepping on each other.

## Phase 0, scaffolding

Done. Solution, entities, `AppDbContext`, JWT and Swagger and CORS setup, empty controllers,
DTOs, frontend pages and `lib/api.ts`, FastAPI health endpoint, Postgres compose file, CI.

No EF migration exists yet. That is task 1.

## Phase 1, the working POS

Tasks are ordered by dependency. Inside a task, backend first, then the page.

1. **Migration and seed.** `dotnet ef migrations add Initial`, then `database update`. Seed one
   admin user, a handful of categories and products, and the single `Setting` row. Seeding lives
   in `AppDbContext` or a small `Seed.cs`, not in a migration.
2. **Auth.** `POST /api/auth/login`, `GET /api/auth/me`. Password hashing, token generation from
   the `Jwt` config section. Frontend: login page stores the token, nav hides links the role
   cannot use, a 401 from `lib/api.ts` sends the user back to login.
3. **Categories and products.** Full CRUD, paging, and search. Frontend: products page with the
   list, search, and a create/edit form. This is the reference task for how a simple CRUD screen
   looks in this repo.
4. **Customers.** Same shape as products, smaller. Good second task for someone new.
5. **Sales.** `POST /api/sales` is the hard one: validate the lines, compute `subtotal`, `total`,
   and `changeAmount`, generate `Number`, and check and decrement stock inside one transaction.
   Frontend: checkout page, then the sales list and detail.
6. **Returns.** `POST /api/sales/{id}/return`, partial and full, stock restored in the same
   transaction, status and `refundedAmount` updated. Frontend: a return action on the sale detail.
7. **Settings and users.** `GET`/`PUT /api/settings` and the `/api/users` routes. Frontend: the
   settings page with language, theme, low stock threshold, and the user list. Theme and language
   are applied on the client from the settings response.
8. **Dashboard.** The three read-only aggregate routes, net of returns. Frontend: the dashboard
   page with plain numbers and a simple table. Charts are explicitly out of scope for phase 1.

Tasks 3 and 4 can run in parallel once 2 is merged. Task 8 needs 5 and 6 finished, or the numbers
lie.

## Phase 2

Not started, not designed. Nothing here is committed to until phase 1 is merged and used.

- **izi-pay.** Today `izipay` is only an accepted `paymentMethod` value that records nothing.
  Phase 2 adds the real payment flow, which means new routes and a change to `docs/API.md`.
- **AI-assisted sale entry.** The FastAPI service in `ai-service/` grows past `GET /health`:
  turn a typed or spoken line like "two large coffees" into sale lines the checkout page can
  confirm. The backend talks to it, the frontend never calls it directly.

## Definition of done

A task is done when all of it is true:

- Every route in that section of `docs/API.md` works with the exact fields and status codes
  listed there, checked in Swagger.
- Validation returns `BadRequest(new { error = "..." })` with a message a cashier could act on.
- The matching page works against a running backend, including the error path.
- No entity is returned from a controller and no DTO field was renamed.
- CI is green.

## Rules that do not change

[AGENTS.md](../AGENTS.md) holds the coding style, the comment rules, and the git workflow.
`docs/API.md` is frozen: if a task cannot be done without changing a route or a field name, stop
and get approval first, then change the contract in the same PR as the code.
