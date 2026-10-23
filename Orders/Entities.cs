namespace Spec_orders_poc.Orders;

public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}

public class Customer
{
    public string CustomerId { get; set; } = string.Empty;
}

public class Order
{
    public long OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
}
