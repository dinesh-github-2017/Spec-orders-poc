# Quickstart / Validation: Customer Order History Search & CSV Export

## Prerequisites
- .NET 8 SDK, Node 20 + Angular CLI
- Entra ID app registrations (API + SPA) with app role `Support`; one test user with the role, one without
- Connection string to the existing orders database (staging) in user secrets: `ConnectionStrings:Orders`
- Known test customer with ?45 orders across statuses; one customer with no orders; one with >10,000 orders (or seeded in staging)

## User secrets (do not commit real values)
```powershell
dotnet user-secrets init --project Spec-orders-poc.csproj
dotnet user-secrets set "ConnectionStrings:Orders" "Server=(localdb)\MSSQLLocalDB;Database=OrdersPoc;Trusted_Connection=True;TrustServerCertificate=True" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:ClientId" "<api-client-id>" --project Spec-orders-poc.csproj
dotnet user-secrets set "AzureAd:Audience" "api://<api-client-id>" --project Spec-orders-poc.csproj
```

## Run

```powershell
dotnet run --project Spec-orders-poc.csproj
cd client; npm install; ng serve
```

## Automated tests
```powershell
dotnet test tests/Spec-orders-poc.Tests --collect:"XPlat Code Coverage"
cd client; ng test --watch=false --code-coverage
```
Expect all green and ?80% coverage on new files.

## Validation scenarios (see [spec.md](./spec.md) and [contract](./contracts/orders-api.openapi.yaml))
| # | Action | Expected |
|---|---|---|
| 1 | Sign in as Support user | "Order History" visible in nav |
| 2 | Sign in as non-Support user | Link hidden; direct API call ? 403 |
| 3 | Call API with no token | 401 |
| 4 | Search unknown customer | 404 / "Customer not found" |
| 5 | Search 45-order customer | 20 rows, 3 pages, newest first |
| 6 | Filter 1–31 May, status Shipped | Only Shipped orders on those days, incl. 31 May 23:59Z |
| 7 | from > to | 400 with message |
| 8 | Customer with no orders | "No orders found"; export = header only |
| 9 | Export 45-order search | 46 lines, ISO 8601 dates, escaped values |
| 10 | Export >10,000 rows | 422; UI asks to narrow filters |
| 11 | Export 10,000 rows on staging | Download completes < 5 s |
