using Microsoft.Extensions.DependencyInjection;
using Spec_orders_poc.Orders;
using Spec_orders_poc.Tests.Infrastructure;

namespace Spec_orders_poc.Tests.Unit;

public class OrderQueryServiceTests : IClassFixture<OrdersApiFactory>
{
    private readonly OrdersApiFactory _factory;

    public OrderQueryServiceTests(OrdersApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<PagedResult<OrderSummary>> SearchAsync(OrderQuery query)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OrderQueryService>();
        return await service.SearchAsync(query);
    }

    [Fact]
    public async Task Range_IsInclusiveWholeUtcDay_IncludingLastMinute()
    {
        var day = new DateOnly(2026, 5, 31);

        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, day, day));

        var item = Assert.Single(result.Items);
        Assert.Equal(new DateTime(2026, 5, 31, 23, 59, 0, DateTimeKind.Utc), item.OrderDate);
    }

    [Fact]
    public async Task FromOnly_IsOpenEnded()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, From: new DateOnly(2026, 5, 22)));

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task ToOnly_IsOpenEnded()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, To: new DateOnly(2026, 5, 1)));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyThatStatus()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, Status: "Shipped"));

        Assert.Equal(12, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("Shipped", i.Status));
    }

    [Fact]
    public async Task Results_AreSortedNewestFirst()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1));

        Assert.Equal(result.Items.OrderByDescending(i => i.OrderDate).ToList(), result.Items);
        Assert.Equal(new DateTime(2026, 5, 31, 23, 59, 0, DateTimeKind.Utc), result.Items[0].OrderDate);
    }

    [Fact]
    public async Task Paging_Uses20PerPage_WithTotals()
    {
        var first = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, Page: 1));
        var last = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, Page: 3));

        Assert.Equal(20, first.Items.Count);
        Assert.Equal(20, first.PageSize);
        Assert.Equal(45, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(5, last.Items.Count);
    }

    [Fact]
    public async Task PageBeyondLast_ReturnsEmptyItems_WithCorrectTotalCount()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerC1, Page: 10));

        Assert.Empty(result.Items);
        Assert.Equal(45, result.TotalCount);
    }

    [Fact]
    public async Task CustomerWithNoOrders_ReturnsEmptyResult()
    {
        var result = await SearchAsync(new OrderQuery(OrdersApiFactory.CustomerEmpty));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task CustomerExists_DistinguishesKnownFromUnknown()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OrderQueryService>();

        Assert.True(await service.CustomerExistsAsync(OrdersApiFactory.CustomerC1));
        Assert.False(await service.CustomerExistsAsync("NOPE"));
    }
}
