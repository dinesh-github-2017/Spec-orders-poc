using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spec_orders_poc.Orders;
using Spec_orders_poc.Tests.Infrastructure;

namespace Spec_orders_poc.Tests.Integration;

public class SearchOrdersTests : IClassFixture<OrdersApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public SearchOrdersTests(OrdersApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "support");
    }

    private async Task<PagedResult<OrderSummary>> GetPageAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummary>>(Json);
        return page!;
    }

    [Fact]
    public async Task C1_ReturnsFirstPageOf20_WithThreePages()
    {
        var page = await GetPageAsync("/api/customers/C1/orders");

        Assert.Equal(20, page.Items.Count);
        Assert.Equal(45, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task Filters_AreApplied()
    {
        var page = await GetPageAsync("/api/customers/C1/orders?from=2026-05-31&to=2026-05-31&status=Shipped");

        var item = Assert.Single(page.Items);
        Assert.Equal("Shipped", item.Status);
    }

    [Fact]
    public async Task CustomerWithNoOrders_Returns200WithEmptyItems()
    {
        var page = await GetPageAsync("/api/customers/C-EMPTY/orders");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task UnknownCustomer_Returns404ProblemJson()
    {
        var response = await _client.GetAsync("/api/customers/NOPE/orders");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task FromAfterTo_Returns400ProblemJson()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders?from=2026-06-02&to=2026-06-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownStatus_Returns400()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders?status=Bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
