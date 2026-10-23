using System.Globalization;
using System.Text;

namespace Spec_orders_poc.Orders;

public sealed class CsvOrderWriter
{
    public const string Header = "OrderId,OrderDate,Total,Status";
    private const string NewLine = "\r\n";

    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly char[] SpecialChars = [',', '"', '\r', '\n'];

    private readonly Stream _stream;
    private bool _preambleWritten;

    public CsvOrderWriter(Stream stream)
    {
        _stream = stream;
    }

    public async Task WriteHeaderAsync(CancellationToken ct = default)
    {
        if (!_preambleWritten)
        {
            _preambleWritten = true;
            var preamble = Utf8WithBom.GetPreamble();
            await _stream.WriteAsync(preamble, ct);
        }

        await WriteLineAsync(Header, ct);
    }

    public Task WriteRowAsync(OrderSummary order, CancellationToken ct = default)
    {
        var line = string.Join(',',
            Escape(order.OrderId),
            Escape(order.OrderDate.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)),
            Escape(order.Total.ToString("0.00", CultureInfo.InvariantCulture)),
            Escape(order.Status));

        return WriteLineAsync(line, ct);
    }

    public static string Escape(string value)
    {
        if (value.IndexOfAny(SpecialChars) >= 0)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private async Task WriteLineAsync(string line, CancellationToken ct)
    {
        var bytes = Utf8WithBom.GetBytes(line + NewLine);
        await _stream.WriteAsync(bytes, ct);
    }
}

