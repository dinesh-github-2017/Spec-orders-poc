# Research: Customer Order History Search & CSV Export

## R1 – Front end
- **Decision**: New Angular app (`client/`) calling the .NET 8 API.
- **Rationale**: Required by PBI 268; confirmed in clarification Q1.
- **Alternatives considered**: Razor Page in existing project (rejected by stakeholder); API only.

## R2 – Authentication & authorization
- **Decision**: Entra ID. Angular uses MSAL (`MsalGuard`, `MsalInterceptor`); API uses `Microsoft.Identity.Web` JWT bearer and a `SupportOnly` policy requiring app role `Support`.
- **Rationale**: Standard SPA+API pattern; 401 from missing/invalid token, 403 from policy failure come for free.
- **Alternatives considered**: Self-issued JWT, cookie Identity, stub auth.
- **Testing**: Replace auth with a test `AuthenticationHandler` in `WebApplicationFactory` to simulate anonymous / non-Support / Support.

## R3 – Data source (OPEN)
- **Decision**: EF Core 8 `OrdersDbContext` mapped to existing tables, `AsNoTracking`, no migrations applied.
- **Rationale**: Clarification Q3 (existing DB, read-only).
- **NEEDS CLARIFICATION**: table/column names, status storage (string vs int code), currency column, connection string, and existing indexes. Until provided, the data model uses logical names.
- **Alternatives considered**: Dapper (viable fallback if EF mapping is awkward), calling another service.

## R4 – Date filtering
- **Decision**: `OrderDate >= from 00:00Z AND OrderDate < (to + 1 day) 00:00Z`; sort `OrderDate DESC, OrderId DESC`.
- **Rationale**: Clarification Q4; half-open upper bound keeps late-day orders and is index-friendly.

## R5 – Paging
- **Decision**: Offset paging (`Skip/Take`), page size fixed at 20, separate `COUNT` query returned as `totalCount`.
- **Rationale**: Small per-customer result sets; simple UI with page numbers.
- **Alternatives considered**: Keyset paging (unnecessary at this scale).

## R6 – CSV export performance
- **Decision**: Count first; if >10,000 return 422 `application/problem+json`. Otherwise stream `IAsyncEnumerable<Order>` from EF straight to the response body via a `StreamWriter`, writing a header then rows. Content-Type `text/csv; charset=utf-8`, `Content-Disposition: attachment; filename="orders-{customerId}-{yyyyMMdd}.csv"`.
- **Rationale**: Streaming keeps memory flat and easily meets <5 s for 10k rows, given an index on (CustomerId, OrderDate).
- **Index**: Recommend `IX_Orders_CustomerId_OrderDate` (INCLUDE Total, Status) — requires DB owner approval.
- **Alternatives considered**: CsvHelper library (fine, but a ~30-line writer avoids a dependency); building the whole file in memory.

## R7 – CSV format
- **Decision**: Columns `OrderId,OrderDate,Total,Status`; dates `yyyy-MM-ddTHH:mm:ssZ`; totals with invariant culture; fields containing `,` `"` CR or LF are wrapped in quotes with `"` doubled; CRLF line endings; UTF-8 with BOM for Excel compatibility.
- **Rationale**: RFC 4180 + ISO 8601 per PBI; BOM so Excel detects UTF-8.

## R8 – Angular download
- **Decision**: Service calls export with `responseType: 'blob'`, observe response, reads filename from `Content-Disposition`, triggers download via object URL. On 422 shows "Too many orders to export (max 10,000). Narrow the filters."
- **Rationale**: Bearer token must be attached by MSAL interceptor, so a plain `<a href>` is not possible.

## R9 – Coverage
- **Decision**: coverlet (`--collect:"XPlat Code Coverage"`) for API; `ng test --code-coverage` for client; 80% threshold on new files.
