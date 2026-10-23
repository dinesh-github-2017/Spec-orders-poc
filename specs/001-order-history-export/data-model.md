# Data Model: Customer Order History Search & CSV Export

Logical model. Physical table/column names are pending from the database owners (research R3). The feature is read-only.

**Interim (stand-in)**: Until the real schema arrives, development uses LocalDB database `OrdersPoc` created by [db/create-stand-in.sql](./db/create-stand-in.sql). Its schema is exported to [db/db-schema.csv](./db/db-schema.csv) (opens in Excel). Stand-in mapping: `dbo.Customers(CustomerId nvarchar(20) PK, CustomerName)`, `dbo.Orders(OrderId bigint PK, CustomerId nvarchar(20) FK, OrderDateUtc datetime2(0), Total decimal(18,2), Currency char(3), Status nvarchar(20) CHECK in Pending/Shipped/Delivered/Cancelled)`, index `IX_Orders_CustomerId_OrderDateUtc`. Re-check T006/T007 when the real schema is supplied.

## Customer (existing)
| Field | Type | Notes |
|---|---|---|
| CustomerId | string/int (TBD) | Primary key; existence check drives 404 |

## Order (existing)
| Field | Type | Notes |
|---|---|---|
| OrderId | string/int (TBD) | Primary key; secondary sort key |
| CustomerId | same as Customer | FK to Customer |
| OrderDate | datetime (UTC) | Filter + primary sort (desc) |
| Total | decimal(18,2) | Displayed as stored |
| Status | enum | Pending, Shipped, Delivered, Cancelled (mapping from DB storage TBD) |

Recommended index: `(CustomerId, OrderDate DESC) INCLUDE (Total, Status)`.

## OrderQuery (request, not persisted)
| Field | Type | Validation |
|---|---|---|
| customerId | route | required |
| from | date (yyyy-MM-dd), optional | valid date |
| to | date (yyyy-MM-dd), optional | valid date; `from <= to` else 400 |
| status | `All` \| `Pending` \| `Shipped` \| `Delivered` \| `Cancelled` | default `All`; unknown ? 400 |
| page | int | ?1, default 1 (search only) |

Page size is fixed at 20.

## OrderSummary (response row)
`orderId`, `orderDate` (ISO 8601 UTC), `total`, `status`

## PagedResult<OrderSummary>
`items[]`, `page`, `pageSize` (20), `totalCount`, `totalPages`

Page beyond last ? empty `items`, correct `totalCount`.

## State transitions
None — the feature does not change order status.
