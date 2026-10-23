using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spec_orders_poc.Orders;
using Spec_orders_poc.Tests.Infrastructure;

namespace Spec_orders_poc.Tests.Integration;

public class ExportOrdersTests : IClassFixture<OrdersApiFactory>
{
    private readonly OrdersApiFactory _factory;
    private readonly HttpClient _client;

    public ExportOrdersTests(OrdersApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "support");
    }

    private static string[] Lines(byte[] bytes)
    {
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        return Encoding.UTF8.GetString(bytes.AsSpan(3)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public async Task C1_ReturnsHeaderPlus45Rows_NewestFirst()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lines = Lines(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(46, lines.Length);
        Assert.Equal("OrderId,OrderDate,Total,Status", lines[0]);
        Assert.Equal("45,2026-05-31T23:59:00Z,99.99,Shipped", lines[1]);
    }

    [Fact]
    public async Task Response_HasCsvContentTypeAndAttachmentFilename()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders/export");

        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.Equal("attachment", disposition?.DispositionType);
        Assert.Equal($"orders-C1-{DateTime.UtcNow:yyyyMMdd}.csv", disposition?.FileName?.Trim('"'));
    }

    [Fact]
    public async Task Filters_AreApplied()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders/export?from=2026-05-31&to=2026-05-31&status=Shipped");

        var lines = Lines(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public async Task CustomerWithNoOrders_ReturnsHeaderOnly()
    {
        var response = await _client.GetAsync("/api/customers/C-EMPTY/orders/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["OrderId,OrderDate,Total,Status"], Lines(await response.Content.ReadAsByteArrayAsync()));
    }

    [Fact]
    public async Task Exactly10000Rows_Returns200()
    {
        var response = await _client.GetAsync("/api/customers/C-10K/orders/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10_001, Lines(await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task MoreThan10000Rows_Returns422ProblemJson()
    {
        var response = await _client.GetAsync("/api/customers/C-BIG/orders/export");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownCustomer_Returns404()
    {
        var response = await _client.GetAsync("/api/customers/NOPE/orders/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FromAfterTo_Returns400()
    {
        var response = await _client.GetAsync("/api/customers/C1/orders/export?from=2026-06-02&to=2026-06-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FailureMidStream_AbortsTheConnection_NotACompleteLookingFile()
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.AddScoped<OrderQueryService, FailingMidStreamService>();
        })).CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "support");

        using var response = await client.GetAsync("/api/customers/C1/orders/export", HttpCompletionOption.ResponseHeadersRead);

        await Assert.ThrowsAnyAsync<Exception>(async () => await response.Content.ReadAsByteArrayAsync());
    }

    private sealed class FailingMidStreamService : OrderQueryService
    {
        public FailingMidStreamService(OrdersDbContext db) : base(db)
        {
        }

        public override async IAsyncEnumerable<OrderSummary> StreamAsync(
            OrderQuery query,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new OrderSummary("1", DateTime.UtcNow, 1m, "Pending");
            await Task.Yield();
            throw new InvalidOperationException("Simulated database failure");
        }
    }
}
