namespace Spec_orders_poc.Orders;

public record OrderSummary(string OrderId, DateTime OrderDate, decimal Total, string Status);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
