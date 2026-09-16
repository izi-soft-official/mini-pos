# Mini POS Frontend — API Connected

This Next.js frontend is configured for the ASP.NET Core Mini POS backend supplied with the project.

## Backend URL

Default API: `http://localhost:5080/api`

Override it with `.env.local`:

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

The backend seed/demo data should provide the accounts documented by the backend. If using the supplied development seed, try:

- admin / admin123
- manager / manager123
- cashier / cashier123

## Important backend status

The supplied backend contains `NotImplementedException` in Dashboard, Users and Settings controllers. The frontend is already wired to those routes, but those pages will show an API-unavailable message until those backend methods are implemented.

The frontend does not store products, customers or sales in localStorage. It only stores the JWT and UI session data locally.
