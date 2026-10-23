---
description: "Task list for Customer Order History Search & CSV Export (ADO PBI 268)"
---

# Tasks: Customer Order History Search & CSV Export

**Input**: Design documents from `/specs/001-order-history-export/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/orders-api.openapi.yaml, quickstart.md

**Tests**: Requested by the spec (SC-005: xUnit for API, Angular service + component tests, ?80% coverage on new code). Write tests first and confirm they fail before implementing.

**Organization**: Tasks are grouped by user story. Story order: US1 (P1 search) ? US3 (P1 access control) ? US2 (P2 export).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = Search, US2 = Export, US3 = Support-only access

## Path Conventions

- API: existing project at repo root (`Controllers/`, `Orders/`, `Program.cs`)
- Angular client: `client/src/app/`
- API tests: `tests/Spec-orders-poc.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Add NuGet packages `Microsoft.EntityFrameworkCore.SqlServer` 8.x and `Microsoft.Identity.Web` to `Spec-orders-poc.csproj`
- [x] T002 [P] Create xUnit project `tests/Spec-orders-poc.Tests/Spec-orders-poc.Tests.csproj` referencing `Spec-orders-poc.csproj`, with packages `Microsoft.AspNetCore.Mvc.Testing` 8.x, `Testcontainers.MsSql` (or SQL Server LocalDB), `coverlet.collector`, and add it to `Spec-orders-poc.sln`
- [x] T003 [P] Scaffold Angular app in `client/` (`ng new client --routing --style=scss`) and add `@azure/msal-browser` and `@azure/msal-angular`
- [x] T004 [P] Add `public partial class Program {}` at end of `Program.cs` so `WebApplicationFactory<Program>` can be used by tests
- [x] T005 [P] Add config sections `AzureAd` (Instance, TenantId, ClientId, Audience) and `ConnectionStrings:Orders` placeholders to `appsettings.json`; document user-secrets usage in `specs/001-order-history-export/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**?? BLOCKER**: T006 requires the existing database schema from the DB owners (research R3). Use logical names from `data-model.md` until supplied.

**Prerequisites (must be resolved before T006/T007)**:

- [x] Schema received: customer/order table and column names, ID types (customer and order), status storage (string or code), currency column, existing indexes � **interim stand-in** `OrdersPoc` on LocalDB, see `db/db-schema.csv`; replace with real schema when supplied
- [x] Connection string for development: `Server=(localdb)\MSSQLLocalDB;Database=OrdersPoc;Trusted_Connection=True;TrustServerCertificate=True` (staging value still needed)

- [ ] T006 Create `OrderStatus` enum (`Pending`, `Shipped`, `Delivered`, `Cancelled`) and read-only entities `Customer` (CustomerId) and `Order` (OrderId, CustomerId, OrderDate UTC, Total decimal(18,2), Status) in `Orders/Entities.cs`
- [ ] T007 Create `OrdersDbContext` in `Orders/OrdersDbContext.cs` mapping entities to existing tables (`ToTable`, column names, status value conversion), `QueryTrackingBehavior.NoTracking`, no migrations
- [ ] T008 Register `OrdersDbContext` with `UseSqlServer(Configuration.GetConnectionString("Orders"))` in `Program.cs`
- [ ] T009 Configure authentication in `Program.cs`: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"))`, add policy `"SupportOnly"` with `RequireRole("Support")`, and call `UseAuthentication()` / `UseAuthorization()` before `MapControllers()`
- [ ] T010 [P] Create test infrastructure in `tests/Spec-orders-poc.Tests/Infrastructure/`: `TestAuthHandler.cs` (reads test header `X-Test-User: anonymous|user|support` to produce no identity / no role / `Support` role) and `OrdersApiFactory.cs` (`WebApplicationFactory<Program>` that swaps auth for `TestAuthHandler` and `OrdersDbContext` for a SQL Server test database (Testcontainers or LocalDB, so date/decimal behaviour matches production) seeded with customers C1 (45 orders over all statuses incl. one at 2026-05-31T23:59:00Z), C-EMPTY (0 orders), C-BIG (10,001 orders), C-10K (10,000 orders))
- [ ] T011 [P] Configure MSAL in `client/src/app/app.config.ts` (`MsalModule`/providers, `MsalInterceptor` protecting `/api/*` with the API scope, `MsalGuard`) using values from `client/src/environments/environment.ts`
- [ ] T012 [P] Create `client/src/app/order-history/order-history.models.ts` with `OrderStatus` union `'All'|'Pending'|'Shipped'|'Delivered'|'Cancelled'`, `OrderSummary { orderId; orderDate; total; status }`, `PagedOrders { items; page; pageSize; totalCount; totalPages }`, `OrderFilter { customerId; from?; to?; status }`

**Checkpoint**: API boots with auth + DbContext; test factory works; Angular app signs in.

---

## Phase 3: User Story 1 � Search a customer's orders (Priority: P1) ?? MVP

**Goal**: Support user enters customer ID (+ optional from/to/status) and sees a 20-per-page grid newest first.

**Independent Test**: Quickstart scenarios 4�8 (search parts).

### Tests for User Story 1 (write first, ensure they fail)

- [ ] T013 [P] [US1] Unit tests for `OrderQuery` validation in `tests/Spec-orders-poc.Tests/Unit/OrderQueryTests.cs`: customerId required; `from <= to` else invalid; status must be one of `All|Pending|Shipped|Delivered|Cancelled` (default `All`); page ?1 default 1
- [ ] T014 [P] [US1] Unit tests for `OrderQueryService.SearchAsync` in `tests/Spec-orders-poc.Tests/Unit/OrderQueryServiceTests.cs`: inclusive UTC whole-day range (`>= from 00:00Z` and `< to+1 00:00Z`, includes 2026-05-31T23:59Z), from-only and to-only open-ended ranges, status filter, sort `OrderDate DESC, OrderId DESC`, page size 20, totalCount/totalPages, page beyond last ? empty items with correct totalCount
- [ ] T015 [P] [US1] Integration tests in `tests/Spec-orders-poc.Tests/Integration/SearchOrdersTests.cs` for `GET /api/customers/{id}/orders` as Support user: C1 ? 20 items/3 pages; filters; C-EMPTY ? 200 with empty items; unknown customer ? 404 problem+json; `from > to` ? 400 problem+json
- [ ] T016 [P] [US1] Angular service spec `client/src/app/order-history/order-history.service.spec.ts` (HttpTestingController): builds `/api/customers/{id}/orders?from&to&status&page`, omits empty params, maps response to `PagedOrders`
- [ ] T017 [P] [US1] Angular component spec `client/src/app/order-history/order-history.component.spec.ts`: customer ID required (search disabled/flagged), renders rows (order ID, date, total, status), paging controls, "No orders found" when empty, "Customer not found" on 404

### Implementation for User Story 1

- [ ] T018 [P] [US1] Create `OrderQuery` record + `Validate()` returning errors in `Orders/OrderQuery.cs` (fields: customerId, `DateOnly? From`, `DateOnly? To`, status string default `All`, page default 1; page size constant 20)
- [ ] T019 [P] [US1] Create DTOs `OrderSummary` and `PagedResult<T>` in `Orders/Dtos.cs` matching `contracts/orders-api.openapi.yaml`
- [ ] T020 [US1] Implement `OrderQueryService` in `Orders/OrderQueryService.cs`: `CustomerExistsAsync`, private `BuildQuery(OrderQuery)` (filters + sort), `SearchAsync` (Count + Skip/Take 20 projecting to `OrderSummary`); register as scoped in `Program.cs`
- [ ] T021 [US1] Implement `CustomerOrdersController` in `Controllers/CustomerOrdersController.cs` with `[ApiController]`, route `api/customers/{id}/orders`, `GET` search action: 400 `ValidationProblem` on invalid query, 404 `Problem` for unknown customer, 200 `PagedResult<OrderSummary>`
- [ ] T022 [US1] Implement `OrderHistoryService.search(filter, page)` in `client/src/app/order-history/order-history.service.ts`
- [ ] T023 [US1] Implement `OrderHistoryComponent` in `client/src/app/order-history/order-history.component.ts|html|scss`: reactive form (customerId required, from, to, status select default All), results table, pager (20/page), "No orders found", "Customer not found", validation message for from > to
- [ ] T024 [US1] Add route `order-history` ? `OrderHistoryComponent` with `MsalGuard` in `client/src/app/app.routes.ts`

**Checkpoint**: Search works end-to-end for a signed-in user.

---

## Phase 4: User Story 3 � Restrict access to Support staff (Priority: P1)

**Goal**: Only signed-in Support users see the nav link or get data (401/403).

**Independent Test**: Quickstart scenarios 1�3.

### Tests for User Story 3

- [ ] T025 [P] [US3] Integration tests in `tests/Spec-orders-poc.Tests/Integration/AuthorizationTests.cs` (theory over both endpoints): anonymous ? 401; signed-in without Support ? 403; Support ? not 401/403
- [ ] T026 [P] [US3] Angular specs `client/src/app/auth/support-role.guard.spec.ts` and `client/src/app/app.component.spec.ts`: guard allows only accounts whose `idTokenClaims.roles` contains `Support`; nav link "Order History" rendered only for Support

### Implementation for User Story 3

- [ ] T027 [US3] Apply `[Authorize(Policy = "SupportOnly")]` to `CustomerOrdersController` in `Controllers/CustomerOrdersController.cs`
- [ ] T028 [P] [US3] Implement `supportRoleGuard` in `client/src/app/auth/support-role.guard.ts` and add it to the `order-history` route in `client/src/app/app.routes.ts`
- [ ] T029 [US3] Add "Order History" link to main navigation in `client/src/app/app.component.html`, shown only when the active account has role `Support` (`client/src/app/app.component.ts`)

**Checkpoint**: Access control verified for API and UI.

---

## Phase 5: User Story 2 � Export matching orders to CSV (Priority: P2)

**Goal**: Export all rows matching current filters (?10,000) as RFC 4180 CSV with ISO 8601 dates.

**Independent Test**: Quickstart scenarios 8�11.

### Tests for User Story 2

- [ ] T030 [P] [US2] Unit tests in `tests/Spec-orders-poc.Tests/Unit/CsvOrderWriterTests.cs`: header `OrderId,OrderDate,Total,Status`; dates `yyyy-MM-ddTHH:mm:ssZ`; invariant-culture totals; values with `,` `"` CR/LF quoted and `"` doubled; CRLF line endings; output starts with a UTF-8 byte-order mark (tests strip it before comparing the header); header-only for empty input
- [ ] T031 [P] [US2] Integration tests in `tests/Spec-orders-poc.Tests/Integration/ExportOrdersTests.cs` for `GET /api/customers/{id}/orders/export`: C1 ? 46 lines in grid order; `Content-Type: text/csv`; `Content-Disposition` attachment filename `orders-{id}-{yyyyMMdd}.csv`; C-EMPTY ? header only; C-10K ? 200; C-BIG ? 422 problem+json; unknown ? 404; `from > to` ? 400; database failure mid-stream ? client read fails (connection aborted), not a complete-looking file (FR-013)
- [ ] T032 [P] [US2] Extend `order-history.service.spec.ts` for `export(filter)`: requests `/orders/export` with `responseType: 'blob'`, parses filename from `Content-Disposition`, surfaces 422
- [ ] T033 [P] [US2] Extend `order-history.component.spec.ts`: Export button disabled until customer ID valid, triggers download, shows "Too many orders to export (max 10,000). Narrow the filters." on 422

### Implementation for User Story 2

- [ ] T034 [P] [US2] Implement `CsvOrderWriter` in `Orders/CsvOrderWriter.cs`: `WriteHeaderAsync`, `WriteRowAsync`, static `Escape(string)` per research R7; UTF-8 with BOM
- [ ] T035 [US2] Add `CountAsync(OrderQuery)` and `StreamAsync(OrderQuery)` (`IAsyncEnumerable<OrderSummary>` via `AsAsyncEnumerable`, same filters/sort as search) to `Orders/OrderQueryService.cs`
- [ ] T036 [US2] Add `GET export` action to `Controllers/CustomerOrdersController.cs`: validate (400), customer exists (404), count > 10,000 ? 422 `Problem`, else set headers and stream rows with `CsvOrderWriter` directly to `Response.Body`; if an exception occurs after streaming starts, log it and call `HttpContext.Abort()` so the download fails (FR-013)
- [ ] T037 [US2] Implement `OrderHistoryService.export(filter)` in `client/src/app/order-history/order-history.service.ts` returning blob + filename
- [ ] T038 [US2] Add Export CSV button to `order-history.component.html` and handler in `order-history.component.ts` that downloads via object URL and shows the 422 message

**Checkpoint**: All three stories work independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T039 [P] Write DB index request for `(CustomerId, OrderDate DESC) INCLUDE (Total, Status)` for DB owner approval in `specs/001-order-history-export/db-index-request.md`
- [ ] T040 [P] Add structured logging (customer ID, filters, row count, duration; no PII values in CSV) to search/export actions in `Controllers/CustomerOrdersController.cs`
- [ ] T041 Run coverage (`dotnet test --collect:"XPlat Code Coverage"`, `ng test --watch=false --code-coverage`) and add tests until new code ?80%
- [ ] T042 Run export performance check on staging for C-10K-sized customer (<5 s) and record result in `specs/001-order-history-export/quickstart.md`
- [ ] T043 Execute all quickstart validation scenarios 1�11 in `specs/001-order-history-export/quickstart.md`

---

## Dependencies & Execution Order

- **Setup (T001�T005)** ? **Foundational (T006�T012)** ? user stories.
- T006/T007 blocked by DB schema from owners (R3).
- **US1** (T013�T024): needs Foundational. MVP.
- **US3** (T025�T029): needs T021 (controller exists) and T029 needs T024; otherwise independent.
- **US2** (T030�T038): needs T020/T021 (shared query + controller); independent of US3.
- **Polish**: after desired stories.
- Within each story: tests ? models/DTOs ? services ? controller/UI.

## Parallel Examples

- **Setup**: T002, T003, T004, T005 together.
- **Foundational**: T010, T011, T012 alongside T006�T009.
- **US1**: T013�T017 (tests) in parallel; then T018 + T019 in parallel.
- **US3**: T025 + T026; then T028 alongside T027.
- **US2**: T030�T033 in parallel; T034 parallel with T035.
- API track (C#) and client track (Angular) can be staffed in parallel throughout.

## Implementation Strategy

1. **MVP**: Setup ? Foundational ? US1 ? validate search (quickstart 4�8).
2. **Secure**: US3 before any staging exposure (order data is sensitive).
3. **Export**: US2, then perf check (T042).
4. **Polish**: coverage, logging, index request, full quickstart run.
