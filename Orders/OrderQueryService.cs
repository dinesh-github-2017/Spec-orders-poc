using Microsoft.EntityFrameworkCore;

namespace Spec_orders_poc.Orders;

public class OrderQueryService
{
    private readonly OrdersDbContext _db;

    public OrderQueryService(OrdersDbContext db)
    {
        _db = db;
    }

    public Task<bool> CustomerExistsAsync(string customerId, CancellationToken ct = default) =>
        _db.Customers.AnyAsync(c => c.CustomerId == customerId, ct);

    public async Task<PagedResult<OrderSummary>> SearchAsync(OrderQuery query, CancellationToken ct = default)
    {
        var filtered = BuildQuery(query);

        var totalCount = await filtered.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)OrderQuery.PageSize);

        var rows = await filtered
            .Skip((query.Page - 1) * OrderQuery.PageSize)
            .Take(OrderQuery.PageSize)
            .Select(o => new { o.OrderId, o.OrderDate, o.Total, o.Status })
            .ToListAsync(ct);

        var items = rows
            .Select(o => new OrderSummary(o.OrderId.ToString(), DateTime.SpecifyKind(o.OrderDate, DateTimeKind.Utc), o.Total, o.Status.ToString()))
            .ToList();

        return new PagedResult<OrderSummary>(items, query.Page, OrderQuery.PageSize, totalCount, totalPages);
    }

    public Task<int> CountAsync(OrderQuery query, CancellationToken ct = default) =>
        BuildQuery(query).CountAsync(ct);

    public virtual async IAsyncEnumerable<OrderSummary> StreamAsync(
        OrderQuery query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var rows = BuildQuery(query)
            .Select(o => new { o.OrderId, o.OrderDate, o.Total, o.Status })
            .AsAsyncEnumerable();

        await foreach (var o in rows.WithCancellation(ct))
        {
            yield return new OrderSummary(
                o.OrderId.ToString(),
                DateTime.SpecifyKind(o.OrderDate, DateTimeKind.Utc),
                o.Total,
                o.Status.ToString());
        }
    }

    private IQueryable<Order> BuildQuery(OrderQuery query)
    {
        var orders = _db.Orders.Where(o => o.CustomerId == query.CustomerId);

        if (query.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            orders = orders.Where(o => o.OrderDate >= start);
        }

        if (query.To is { } to)
        {
            var endExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            orders = orders.Where(o => o.OrderDate < endExclusive);
        }

        if (query.Status != "All" && Enum.TryParse<OrderStatus>(query.Status, out var status))
        {
            orders = orders.Where(o => o.Status == status);
        }

        return orders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderId);
    }
}
