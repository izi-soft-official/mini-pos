# Mini POS Frontend

A Next.js (App Router) frontend for the ASP.NET Core Mini POS backend. It handles authentication, products, categories, customers, checkout, sales history, a dashboard, user management, and settings — all through a typed REST client.

## Stack

- **Next.js 15** (App Router) + **React 19**, all pages are client components (`"use client"`)
- **TypeScript**
- **Tailwind CSS** for styling, with a small set of reusable utility classes (`.card`, `.input`, `.btn-primary`, etc.) defined in `app/globals.css` (npm install @tailwindcss/postcss postcss)
- **lucide-react** for icons, re-exported through `components/Icons.tsx` (npm install lucide-react)

## Backend connection

Default API: `http://localhost:5080/api`

Override it with a `.env.local` file (see `.env.local.example`):

```env
NEXT_PUBLIC_API_URL=http://localhost:5080/api
```

## Run

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Demo accounts

Provided by the backend's seed data:

- admin / admin123

## Important backend status

The supplied backend throws `NotImplementedException` in the Dashboard, Users, and Settings controllers. The frontend is already wired to those routes, but those pages will show an "API unavailable" message until those backend methods are implemented.

The frontend does not store products, customers, or sales in `localStorage`. It only stores the JWT and the logged-in user's basic info locally, under the keys `mini-pos-token` and `mini-pos-user`.

## Project structure

```
app/                  Pages (Next.js App Router)
components/           Shared UI and app-wide logic
lib/                  API client and shared types
```

### `app/` — pages

Each folder under `app/` is a route, and each contains a single `page.tsx`. All are client components that fetch their own data on mount via `useEffect` and call functions exported from `lib/api.ts`.

| File | Route | What it does |
|---|---|---|
| `app/layout.tsx` | (root layout) | Wraps every page in `Providers` (auth/session context) and `AppShell` (sidebar + header chrome). Sets the page `<title>`/metadata. |
| `app/login/page.tsx` | `/login` | Username/password form. Calls `login()` from `Providers`, then redirects to `/`. Not wrapped by the authenticated shell (see `AppShell`). |
| `app/page.tsx` | `/` | Dashboard. Shows today's sales count/revenue/average sale, low-stock count, a 7-day sales bar chart, and a top-products list. Gated behind the `dashboard` permission. |
| `app/products/page.tsx` | `/products` | Product list with search, category filter, and a "low stock" toggle. Add/edit product via a modal (`ProductModal`, defined in the same file). Links to `/categories` via a "Manage categories" button. Write actions gated behind the `productWrite` permission. |
| `app/categories/page.tsx` | `/categories` | Category CRUD (create/edit/deactivate). Has a back button to `/products`. |
| `app/checkout/page.tsx` | `/checkout` | Point-of-sale screen: search/add products to a cart, pick a customer, choose payment method, apply a discount, and submit the sale. |
| `app/sales/page.tsx` | `/sales` | Sales history list with date-range and customer filters. Clicking a row opens a detail modal (`SaleModal`, same file) showing line items, totals, payment, and change. |
| `app/customers/page.tsx` | `/customers` | Customer CRUD with search, plus a "view" modal showing a customer's total spend (pulled from `sales.list({ customerId })`). |
| `app/users/page.tsx` | `/users` | Admin-only user management (create/edit users, set role, reset password). |
| `app/settings/page.tsx` | `/settings` | Language, theme, and low-stock threshold, persisted via the backend. |
| `app/globals.css` | — | Tailwind entrypoint plus the shared component classes (`.card`, `.input`, `.btn-*`, `.navlink`, `.table`, `.stat`, `.badge`) used across all pages instead of repeating Tailwind utility strings everywhere. |

### `components/` — shared UI and app logic

| File | What it does |
|---|---|
| `Providers.tsx` | Holds the authentication/session context (`AppContext`). Loads the current user on mount if a token exists, exposes `login`, `logout`, and `can(permission)` — a role-based permission check (`Admin`/`Manager`/`Cashier`) used throughout the app to show/hide buttons, nav links, and whole pages. Exported via the `useApp()` hook. |
| `AppShell.tsx` | The authenticated layout: redirects to `/login` if there's no user, otherwise renders the `Sidebar`, a header (mobile hamburger button, current user's name/role, logout button), and the page content. Owns the mobile sidebar's open/closed state and passes it down to `Sidebar`. |
| `Sidebar.tsx` | Left navigation. Filters the link list by `can(permission)` so users only see pages they have access to. On desktop it's a static docked panel; on mobile (`< md`) it becomes a slide-in drawer controlled by the `open`/`onClose` props passed from `AppShell`, with a backdrop and auto-close on route change. |
| `Icons.tsx` | Re-exports the specific `lucide-react` icons used in the app, so every other file imports icons from one local module instead of `lucide-react` directly. |
| `StatCard.tsx` | Small reusable card (title, value, icon, optional note) for stat/metric displays. |

### `lib/` — API client and types

| File | What it does |
|---|---|
| `api.ts` | The HTTP client. A single `request()` helper wraps `fetch`, attaches the JWT from `localStorage` as a Bearer token, sets JSON headers, and handles 401s (clears auth, redirects to `/login`) and non-OK responses (throws with the backend's error message). Everything else in the file is a small set of typed functions grouped by resource — `auth`, `categories`, `products`, `customers`, `sales`, `dashboard`, `users`, `settings` — each calling `request()` against the matching backend route. This is the only place `fetch` is called; pages never call the API directly. |
| `types.ts` | Shared TypeScript types matching the backend's shapes: `User`, `Category`, `Product`, `Customer`, `Sale`/`SaleItem`/`SaleListItem`, `PagedResponse<T>`, `Settings`, `DashboardSummary`, `TopProduct`, `SalesByDay`. |
| `db.ts` | **Legacy/unused.** Leftover seed data and `localStorage` helpers from an earlier, backend-less version of the app. Its types (`id: "u1"`, `category: string`, etc.) no longer match `types.ts`, and nothing in `app/` or `components/` imports from it. Safe to delete, kept here only as a historical artifact. |
