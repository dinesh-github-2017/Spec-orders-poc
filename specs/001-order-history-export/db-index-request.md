# DB Index Request: Customer Order History (ADO PBI 268)

**For approval by**: database owners
**Feature**: Order history search and CSV export (read-only)

## Request
Create a covering index on the orders table to support the search and export queries:

```sql
CREATE INDEX IX_Orders_CustomerId_OrderDateUtc
    ON dbo.Orders (CustomerId, OrderDateUtc DESC)
    INCLUDE (Total, Status);
```

Table and column names are taken from the interim stand-in schema (`db/db-schema.csv`). Replace them with the real names when the production schema is confirmed. The stand-in database `OrdersPoc` already has this index.

## Why
- Every query filters on `CustomerId`, optionally on an `OrderDateUtc` range and `Status`, and sorts by `OrderDateUtc DESC, OrderId DESC`.
- The key `(CustomerId, OrderDateUtc DESC)` serves the filter and the sort without a separate sort step.
- `INCLUDE (Total, Status)` covers the returned columns, so no key lookups are needed. `OrderId` is the clustered key and is carried in the index automatically.
- It supports the NFR: export of up to 10,000 rows in under 5 seconds on staging.

## Impact
- Read performance: improves search paging (`COUNT` plus `OFFSET/FETCH`) and export streaming.
- Write cost: a small increase on `INSERT` and `UPDATE` of `Status`, `Total` or `OrderDateUtc` for the orders table.
- Storage: roughly the size of `CustomerId`, `OrderDateUtc`, `Total` and `Status` per row, plus overhead. Estimate with real row counts before approval.

## Checks before approving
- Confirm whether an equivalent index already exists. If so, no change is needed.
- Confirm the real column types (`CustomerId`, `OrderId`) and the status storage (string or code).
- Create it first in staging and compare the query plan and export timing (T042).
