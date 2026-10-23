using System.Net;
using Spec_orders_poc.Tests.Infrastructure;

namespace Spec_orders_poc.Tests.Integration;

public class AuthorizationTests : IClassFixture<OrdersApiFactory>
{
    private readonly OrdersApiFactory _factory;

    public AuthorizationTests(OrdersApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string> Endpoints => new()
    {
        "/api/customers/C1/orders",
        "/api/customers/C1/orders/export"
    };

    private async Task<HttpStatusCode> GetAsync(string url, string? user)
    {
        var client = _factory.CreateClient();
        if (user is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, user);
        }

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        return response.StatusCode;
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Anonymous_Returns401(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, await GetAsync(url, "anonymous"));

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task NoHeader_Returns401(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, await GetAsync(url, null));

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task SignedInWithoutSupportRole_Returns403(string url) =>
        Assert.Equal(HttpStatusCode.Forbidden, await GetAsync(url, "user"));

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task SupportRole_IsAllowed(string url)
    {
        var status = await GetAsync(url, "support");

        Assert.NotEqual(HttpStatusCode.Unauthorized, status);
        Assert.NotEqual(HttpStatusCode.Forbidden, status);
    }
}
