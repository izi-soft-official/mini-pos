# mini-pos

Training project for IZI Soft's dev team - a scaled down POS (products, sales, customers, settings, izi-pay payments, AI-assisted sale entry). Not for production use.

## Run it

Needs the .NET 10 SDK, Node 20 or later, and Docker.

Database:

```
docker compose up -d
```

Backend, on http://localhost:5080 with Swagger at `/swagger`:

```
cd backend/MiniPos.Api
dotnet run
```

Frontend, on http://localhost:3000:

```
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

AI service, phase 2, health endpoint only:

```
cd ai-service
pip install -r requirements.txt
uvicorn main:app --port 8000
```

## Docs

`docs/API.md` is the frozen contract for every route and field. `docs/PLAN.md` is the build plan.
Working rules for the team and for coding agents are in [AGENTS.md](AGENTS.md).
