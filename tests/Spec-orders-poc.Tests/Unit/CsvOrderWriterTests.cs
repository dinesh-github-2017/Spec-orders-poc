using System.Text;
using Spec_orders_poc.Orders;

namespace Spec_orders_poc.Tests.Unit;

public class CsvOrderWriterTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    private static async Task<byte[]> WriteAsync(params OrderSummary[] orders)
    {
        using var stream = new MemoryStream();
        var writer = new CsvOrderWriter(stream);
        await writer.WriteHeaderAsync();
        foreach (var o in orders)
        {
            await writer.WriteRowAsync(o);
        }

        return stream.ToArray();
    }

    private static string StripBom(byte[] bytes) =>
        Encoding.UTF8.GetString(bytes.AsSpan(3));

    private static OrderSummary Order(string id = "1", decimal total = 10m, string status = "Shipped") =>
        new(id, new DateTime(2026, 5, 31, 23, 59, 0, DateTimeKind.Utc), total, status);

    [Fact]
    public async Task OutputStartsWithUtf8Bom()
    {
        var bytes = await WriteAsync();

        Assert.Equal(Bom, bytes[..3]);
    }

    [Fact]
    public async Task EmptyInput_ProducesHeaderOnly()
    {
        var text = StripBom(await WriteAsync());

        Assert.Equal("OrderId,OrderDate,Total,Status\r\n", text);
    }

    [Fact]
    public async Task Row_UsesIsoDateAndInvariantTotal()
    {
        var text = StripBom(await WriteAsync(Order("42", 1234.5m)));

        Assert.Equal("OrderId,OrderDate,Total,Status\r\n42,2026-05-31T23:59:00Z,1234.50,Shipped\r\n", text);
    }

    [Fact]
    public async Task LocalDateTime_IsConvertedToUtc()
    {
        var local = new OrderSummary("1", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), 1m, "Pending");

        var text = StripBom(await WriteAsync(local));

        Assert.Contains("2026-01-01T12:00:00Z", text);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    [InlineData("line1\r\nline2", "\"line1\r\nline2\"")]
    [InlineData("", "")]
    public void Escape_FollowsRfc4180(string input, string expected) =>
        Assert.Equal(expected, CsvOrderWriter.Escape(input));

    [Fact]
    public async Task Row_EscapesSpecialCharactersInFields()
    {
        var text = StripBom(await WriteAsync(Order("1,2", 5m, "Ship \"now\"")));

        Assert.Contains("\"1,2\",2026-05-31T23:59:00Z,5.00,\"Ship \"\"now\"\"\"\r\n", text);
    }

    [Fact]
    public async Task LinesEndWithCrLf()
    {
        var text = StripBom(await WriteAsync(Order("1"), Order("2")));

        Assert.Equal(3, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty));
    }
}
