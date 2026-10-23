# Feature Specification: Customer Order History Search & CSV Export

**Feature Branch**: `001-order-history-export`

**Created**: 2026-10-16

**Status**: Draft

**Input**: User description: "ADO PBI 268 — As a customer service representative, I want to search a customer's orders by date range and status and export the results to CSV, so that I can answer customer queries quickly and share the data."

## Clarifications

### Session 2026-10-16

- Q: Should the Order History screen be built in Angular (as the PBI says) or as a Razor Page in this project? ? A: Angular, as in the PBI — a new Angular app that calls the .NET 8 API.
- Q: How should the Angular app sign users in and prove to the API that they have the Support role? ? A: Microsoft Entra ID — Angular signs in with MSAL; the API checks the bearer token for a `Support` app role.
- Q: Where should the API get customer and order data from? ? A: An existing database that already holds customers and orders; accessed read-only without changing its tables.
- Q: How should the from and to dates filter orders? ? A: Whole days, both included, compared in UTC; results sorted newest first, then by order ID.
- Q: What should happen when a CSV export would contain more than 10,000 rows? ? A: Reject the export with an error and ask the user to narrow the filters.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search a customer's orders (Priority: P1)

A Support representative opens Order History from the main navigation, enters a customer ID, optionally a date range and a status, and sees the matching orders in a paged grid (20 per page) showing order ID, date, total and status.

**Why this priority**: Answering customer queries quickly is the core value; export depends on the same search.

**Independent Test**: Sign in as a Support user, search a known customer with and without filters, and confirm the grid contents and paging.

**Acceptance Scenarios**:

1. **Given** a Support user and a customer with 45 orders, **When** they search by customer ID only, **Then** page 1 shows 20 orders newest first and 3 pages are available.
2. **Given** the from date 2026-05-01 and to date 2026-05-31, **When** they search, **Then** only orders dated on or between those days (UTC) are shown, including orders late on 31 May.
3. **Given** status "Shipped", **When** they search, **Then** only Shipped orders are shown; "All" shows every status.
4. **Given** no orders match, **When** they search, **Then** the screen shows "No orders found".
5. **Given** the customer ID is empty, **When** they try to search, **Then** the search is blocked and the field is flagged as required.
6. **Given** a customer ID that does not exist, **When** they search, **Then** a "Customer not found" message is shown.

---

### User Story 2 - Export matching orders to CSV (Priority: P2)

From the same screen, the representative exports all orders matching the current filters (not only the visible page) to a CSV file they can share.

**Why this priority**: Enables sharing data, but builds on the search in Story 1.

**Independent Test**: Run a search spanning several pages, export, and compare the file's rows with the full result set.

**Acceptance Scenarios**:

1. **Given** a search returning 45 orders, **When** they export, **Then** the file has a header row plus 45 data rows in the same order as the grid.
2. **Given** dates in the results, **When** exported, **Then** dates are in ISO 8601 format.
3. **Given** a value containing a comma, quote or line break, **When** exported, **Then** it is quoted and inner quotes are doubled so the file opens correctly in a spreadsheet.
4. **Given** no orders match, **When** they export, **Then** the file contains only the header row.
5. **Given** more than 10,000 matching orders, **When** they export, **Then** the export is refused and the user is asked to narrow the filters.

---

### User Story 3 - Restrict access to Support staff (Priority: P1)

Only signed-in users in the Support role can see the Order History link or retrieve order data.

**Why this priority**: Order data is customer-sensitive; access control must ship with the first release.

**Independent Test**: Call the search and export as an anonymous user, a signed-in non-Support user, and a Support user.

**Acceptance Scenarios**:

1. **Given** a user who is not signed in, **When** they request search or export, **Then** they receive an "unauthenticated" response (401).
2. **Given** a signed-in user without the Support role, **When** they request search or export, **Then** they receive a "forbidden" response (403) and do not see the navigation link.
3. **Given** a Support user and an unknown customer ID, **When** they request search or export, **Then** they receive a "not found" response (404).

---

### Edge Cases

- From date later than to date ? request rejected as invalid with a clear message.
- Only one of from/to supplied ? open-ended range on the other side.
- Page number beyond the last page ? empty page with correct total count (not an error).
- Export of exactly 10,000 rows ? allowed; 10,001 ? refused.
- Orders with identical timestamps ? ordered by order ID so paging and export are stable.
- Database unavailable ? user sees a generic error; no partial CSV is presented as complete.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an Order History screen reachable from the main navigation, visible only to Support role users.
- **FR-002**: Users MUST supply a customer ID; from date, to date and status are optional.
- **FR-003**: Status filter MUST offer All, Pending, Shipped, Delivered, Cancelled (default All).
- **FR-004**: Date filtering MUST treat from/to as whole UTC days, both inclusive.
- **FR-005**: Results MUST be paged at 20 per page, sorted by order date descending then order ID descending, showing order ID, date, total and status, plus the total match count.
- **FR-006**: System MUST show "No orders found" when nothing matches.
- **FR-007**: Export MUST include all rows matching current filters, in the same order as the grid, with a header row.
- **FR-008**: Export MUST use ISO 8601 dates and RFC 4180-style escaping (quote fields containing comma, quote or line break; double inner quotes).
- **FR-009**: Export with no matches MUST return a header-only file.
- **FR-010**: Export MUST be refused when more than 10,000 rows match, with a message asking the user to narrow filters.
- **FR-011**: Search and export MUST return 401 when unauthenticated, 403 when not in the Support role, 404 for an unknown customer, and 400 for invalid filters.
- **FR-012**: The feature MUST only read order data; it MUST NOT modify the existing database.
- **FR-013**: If an export fails after the download has started, the download MUST fail visibly rather than produce a truncated file that looks complete.

### Key Entities

- **Customer**: Identified by customer ID; existence determines 404 responses.
- **Order**: Order ID, customer ID, order date (UTC), total amount, status (Pending, Shipped, Delivered, Cancelled).
- **Order search criteria**: Customer ID, optional from/to date, status, page number.
- **Paged order result**: Page of orders, page number, page size (20), total count.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Exporting up to 10,000 rows completes in under 5 seconds on staging.
- **SC-002**: A Support user can find a customer's orders for a given month and status in under 30 seconds.
- **SC-003**: 100% of exported files open correctly in a spreadsheet with values matching the on-screen results.
- **SC-004**: 100% of non-Support or anonymous requests are denied.
- **SC-005**: Automated tests exist for the API and for the UI service and component, with at least 80% coverage on new code.

## Assumptions

- The front end is a new Angular app; the back end is a .NET 8 API (per clarification Q1).
- Users sign in with Microsoft Entra ID; Support membership is an app role in the token (Q2).
- Customer and order data come from an existing database; schema and connection details will be provided before planning (Q3).
- Order totals are displayed in the currency stored with the order; no currency conversion.
- A supporting index on customer ID and order date may be needed to meet SC-001; this requires database owner approval.
- Editing orders and exporting formats other than CSV are out of scope.
