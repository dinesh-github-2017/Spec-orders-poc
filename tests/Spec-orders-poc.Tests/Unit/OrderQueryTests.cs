using Spec_orders_poc.Orders;

namespace Spec_orders_poc.Tests.Unit;

public class OrderQueryTests
{
    [Fact]
    public void Defaults_AreStatusAllAndPageOne()
    {
        var query = new OrderQuery("C1");

        Assert.Equal("All", query.Status);
        Assert.Equal(1, query.Page);
        Assert.Empty(query.Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CustomerId_IsRequired(string customerId)
    {
        var errors = new OrderQuery(customerId).Validate();

        Assert.Contains("id", errors.Keys);
    }

    [Fact]
    public void From_AfterTo_IsInvalid()
    {
        var query = new OrderQuery("C1", new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 1));

        Assert.Contains("from", query.Validate().Keys);
    }

    [Fact]
    public void From_EqualToTo_IsValid()
    {
        var day = new DateOnly(2026, 6, 1);

        Assert.Empty(new OrderQuery("C1", day, day).Validate());
    }

    [Fact]
    public void OpenEndedRanges_AreValid()
    {
        Assert.Empty(new OrderQuery("C1", From: new DateOnly(2026, 6, 1)).Validate());
        Assert.Empty(new OrderQuery("C1", To: new DateOnly(2026, 6, 1)).Validate());
    }

    [Theory]
    [InlineData("All")]
    [InlineData("Pending")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void KnownStatuses_AreValid(string status)
    {
        Assert.Empty(new OrderQuery("C1", Status: status).Validate());
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("shipped")]
    [InlineData("")]
    public void UnknownStatus_IsInvalid(string status)
    {
        Assert.Contains("status", new OrderQuery("C1", Status: status).Validate().Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_BelowOne_IsInvalid(int page)
    {
        Assert.Contains("page", new OrderQuery("C1", Page: page).Validate().Keys);
    }
}
