# Smart Expense Tracker

Separate-stack MVP implementation:

- `ExpenseTracker.Api`: ASP.NET Core Web API with PostgreSQL, JWT auth, custom categories, analytics, and Gemini-backed category suggestion.
- `expense-tracker-web`: React + Vite frontend in JavaScript.
- `ExpenseTracker.Tests`: integration tests for auth, categories, expenses, analytics, and graceful AI failure.

## Prerequisites

- .NET SDK 9
- Node.js 22+
- PostgreSQL

## Backend setup

1. Create a PostgreSQL database named `expense_tracker`, or update the connection string in `ExpenseTracker.Api/appsettings.json`.
2. Optional but recommended: set `Gemini__ApiKey` as an environment variable for live AI suggestions.
3. Run the API:

```powershell
dotnet run --project .\ExpenseTracker.Api\ExpenseTracker.Api.csproj
```

The API defaults to `http://localhost:5134` and applies migrations on startup.

## Frontend setup

1. Install dependencies if needed:

```powershell
cd .\expense-tracker-web
npm install
```

2. Optional: set `VITE_API_BASE_URL` if the API is not running at `http://localhost:5134`.
3. Start the frontend:

```powershell
npm run dev
```

## Tests

Run API integration tests:

```powershell
dotnet test .\ExpenseTracker.Tests\ExpenseTracker.Tests.csproj
```

Run frontend lint/build checks:

```powershell
cd .\expense-tracker-web
npm run lint
npm run build
```
