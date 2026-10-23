# Spec Orders POC

Customer order history search and CSV export for support staff (Azure DevOps PBI 268).

A support user enters a customer ID, optionally narrows by date range and status, and browses that customer's orders 20 per page, newest first. They can export every order matching the filters as a CSV file. Only users with the `Support` role can see the screen or call the API.

## Features

- **Search:** filter by customer ID (required), from date, to date and status (`All`, `Pending`, `Shipped`, `Delivered`, `Cancelled`). Date ranges are inclusive whole UTC days.
- **Export:** downloads all matching rows (up to 10,000) as RFC 4180 CSV with a header row, ISO 8601 UTC dates and UTF-8 BOM. Larger result sets return `422`.
- **Access control:** Microsoft Entra ID bearer tokens. The API returns `401` when unauthenticated, `403` without the `Support` role, and `404` for an unknown customer.

## Tech stack

| Part | Technology |
|---|---|
| API | ASP.NET Core (.NET 8), Entity Framework Core (SQL Server), Microsoft.Identity.Web |
| Client | Angular with MSAL (`@azure/msal-angular`) |
| API tests | xUnit, `WebApplicationFactory`, SQL Server LocalDB |
| Client tests | Vitest via `ng test` |

## Repository layout

```
Controllers/      API controllers
Orders/           Entities, DbContext, query service, CSV writer, DTOs
client/           Angular app
tests/            xUnit tests
specs/            Spec Kit feature docs: spec, plan, tasks, API contract, DB stand-in
```

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/customers/{id}/orders` | Paged, filtered search. Query: `from`, `to`, `status`, `page`. |
| GET | `/api/customers/{id}/orders/export` | CSV of all matching orders. Query: `from`, `to`, `status`. |

The full contract is in [`specs/001-order-history-export/contracts/orders-api.openapi.yaml`](specs/001-order-history-export/contracts/orders-api.openapi.yaml).

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js and npm (a recent LTS release)
- SQL Server LocalDB (Windows) for the development database and the integration tests
- A Microsoft Entra ID tenant with an API app registration, a SPA app registration and an app role named `Support`

### Configure

Keep real values out of source control. Use user secrets for the API:

```powershell
dotnet user-secrets init --project Spec-orders-poc.csproj
dotnet user-secrets set "ConnectionStrings:Orders" "Server=(localdb)\MSSQLLocalDB;Database=OrdersPoc;Trusted_Connection=True;TrustServerCertificate=True" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:ClientId" "<api-client-id>" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:Audience" "api://<api-client-id>" --project Spec-orders-poc.csproj
```

Set the SPA values (`clientId`, `authority`, `redirectUri`, `apiScope`, `apiBaseUrl`) in `client/src/environments/environment.ts`.

The development database is a stand-in schema. Create it with `specs/001-order-history-export/db/create-stand-in.sql`.

### Run

```powershell
dotnet run --project Spec-orders-poc.csproj
cd client
npm install
npm start
```

## Tests

```powershell
dotnet test tests/Spec-orders-poc.Tests --collect:"XPlat Code Coverage"
cd client
npm test -- --watch=false
```

The integration tests create and drop a temporary LocalDB database, seeded with customers `C1` (45 orders), `C-EMPTY` (none), `C-BIG` (10,001 orders) and `C-10K` (10,000 orders). A test user is selected with the `X-Test-User` header (`anonymous`, `user` or `support`).

## Continuous integration

GitHub Actions (`.github/workflows/ci.yml`) builds and tests the API on Windows and the Angular client on Linux for every push and pull request to `main`.

## Documentation

Design documents are in [`specs/001-order-history-export/`](specs/001-order-history-export/): `spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md` and `tasks.md`.

## Status

The API, search, access control and export are implemented and covered by passing C# tests. The Angular client is written but its build and specs have not yet been verified. Real Entra ID values and the staging checks (export performance, quickstart scenarios) are still outstanding.
