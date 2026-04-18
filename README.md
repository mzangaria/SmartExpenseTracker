# Smart Expense Tracker

Separate-stack MVP implementation:

- `ExpenseTracker.Api`: ASP.NET Core Web API with PostgreSQL, JWT auth, custom categories, analytics, and Gemini-backed category suggestion.
- `expense-tracker-web`: React + Vite frontend in JavaScript.
- `ExpenseTracker.Tests`: integration tests for auth, categories, expenses, analytics, and graceful AI failure.

All expenses are stored and reported in `ILS`. Currency is system-managed and not user-configurable.

## Prerequisites

- .NET SDK 9.0.305 via `global.json`
- Node.js 22+
- PostgreSQL

## Backend setup

1. Provide secrets through environment variables, `dotnet user-secrets`, or a secret manager. Do not store real credentials in `appsettings.json`.
2. Set `ConnectionStrings__DefaultConnection` or `DATABASE_URL` for PostgreSQL.
3. Set `Jwt__Key` to a strong random value with at least 32 characters.
4. Optional: set `Gemini__ApiKey` for live AI suggestions and Telegram expense parsing fallback.
5. Create a PostgreSQL database named `expense_tracker` if your connection string points to it.
6. Run the API:

```powershell
dotnet run --project .\ExpenseTracker.Api\ExpenseTracker.Api.csproj
```

The API defaults to `http://localhost:5134` and applies migrations on startup.

### Telegram expense bot setup

1. In Telegram, open BotFather, run `/newbot`, choose a name and username, and copy the bot token.
2. Configure secrets with environment variables, user-secrets, or your secret manager:

```powershell
$env:Telegram__BotToken="123456:bot-token"
$env:Telegram__BotUsername="YourExpenseBot"
$env:Telegram__EnablePolling="true"
$env:Telegram__PollingIntervalSeconds="2"
$env:Telegram__LinkTokenMinutes="10"
$env:Gemini__ApiKey="your-google-ai-studio-key"
$env:Gemini__ExpenseParsingModel="gemini-2.5-flash"
```

3. Start the API. With `Telegram__EnablePolling=true`, the API process polls Telegram for private chat messages.
4. Start the frontend, log in, open Profile, and click **Connect Telegram**.
5. Open the generated Telegram link. The bot receives `/start <token>` and links that private chat to your app user.
6. Send messages such as `coffee 18`, `spent 42 on lunch`, `uber 65 yesterday`, or `rent 3200 category housing`.

The bot rejects group chats and unlinked private chats. It stores processed Telegram update IDs for idempotency and writes ingestion logs with the original message, parser type, parsed JSON, confidence, status, and created expense ID. Gemini calls are made only by the backend using `Gemini__ApiKey`; no Gemini key or call is exposed to client-side code.

Supported bot commands:

- `/help`: show examples.
- `/last`: show the last expense created from Telegram.
- `/undo`: remove the last expense created from Telegram.

The parser uses deterministic rules first and calls Gemini structured JSON parsing only when deterministic parsing is incomplete or low confidence. Change `Gemini__ExpenseParsingModel` to switch parsing models without code changes.

## Frontend setup

1. Install dependencies if needed:

```powershell
cd .\expense-tracker-web
npm ci
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

## CI and Merge Policy

- CI workflow: `.github/workflows/ci.yml`
- Required checks before merge:
  - `backend-test`
  - `frontend-lint-build`
- Protect the default branch in GitHub and require those exact checks before merge.
- Keep local tooling aligned with CI:
  - use the pinned .NET SDK from `global.json`
  - use Node.js 22
  - use `npm ci` instead of `npm install`
