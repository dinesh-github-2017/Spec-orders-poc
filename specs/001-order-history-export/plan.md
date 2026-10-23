# Implementation Plan: Customer Order History Search & CSV Export

**Branch**: `001-order-history-export` | **Date**: 2026-10-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-order-history-export/spec.md` (ADO PBI 268)

## Summary

Support representatives search a customer's orders (customer ID required; optional UTC whole-day date range and status) in a paged grid of 20 rows, and export all matching rows (max 10,000) to RFC 4180 CSV. Delivered as a new Angular client calling two secured endpoints on the existing .NET 8 ASP.NET Core project, which reads orders read-only from an existing database via EF Core. Access is limited to the Entra ID `Support` app role.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (API); TypeScript 5 / Angular 17+ (client)

**Primary Dependencies**: ASP.NET Core 8 controllers, EF Core 8 (SQL Server provider), Microsoft.Identity.Web; Angular, @azure/msal-angular

**Storage**: Existing SQL Server database (read-only) — schema, table/column names and connection details NEEDS CLARIFICATION (owner input; see research R3)

**Testing**: xUnit + WebApplicationFactory + test auth handler (API); Jasmine/Karma via `ng test` (Angular); coverlet + Angular coverage ?80% on new code

**Target Platform**: Azure-hosted Linux/Windows web app (staging + production); evergreen browsers

**Project Type**: Web application (API + SPA)

**Performance Goals**: Export ?10,000 rows in <5 s on staging; paged search <1 s p95

**Constraints**: Read-only DB access; streamed CSV (no full in-memory buffering); export capped at 10,000 rows (422 above)

**Scale/Scope**: Two endpoints, one Angular screen, one nav link; tens of concurrent Support users

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled template — no ratified principles exist, so there are no gates to evaluate. **Result: PASS (vacuous)**. Recommend running `/speckit-constitution` before `/speckit-tasks`.

Post-design re-check: PASS (no principles to violate; design adds no unjustified complexity).

## Project Structure

### Documentation (this feature)

```text
specs/001-order-history-export/
??? plan.md
??? research.md
??? data-model.md
??? quickstart.md
??? contracts/
?   ??? orders-api.openapi.yaml
??? checklists/requirements.md
??? tasks.md             # created by /speckit-tasks
```

### Source Code (repository root)

```text
Spec-orders-poc/                    # existing .NET 8 project – hosts the API
??? Controllers/
?   ??? CustomerOrdersController.cs
??? Orders/
?   ??? OrdersDbContext.cs          # read-only mapping to existing tables
?   ??? OrderQuery.cs               # filter + validation
?   ??? OrderQueryService.cs        # paging, count, streaming
?   ??? CsvOrderWriter.cs           # RFC 4180 escaping, ISO 8601 dates
??? Program.cs                      # auth (Microsoft.Identity.Web), "SupportOnly" policy, DbContext

client/                             # new Angular app
??? src/app/
    ??? auth/                       # MSAL config, role guard
    ??? order-history/
    ?   ??? order-history.component.ts|html|spec.ts
    ?   ??? order-history.service.ts|spec.ts
    ??? app.component (nav link shown for Support role)

tests/
??? Spec-orders-poc.Tests/          # xUnit: unit (CSV, query) + integration (WebApplicationFactory)
```

**Structure Decision**: Reuse the existing `Spec-orders-poc` project as the API host (it already has Controllers), add an Angular app in `client/`, and an xUnit project in `tests/`. Existing Razor/MVC views are untouched.

## Complexity Tracking

No violations to justify.
