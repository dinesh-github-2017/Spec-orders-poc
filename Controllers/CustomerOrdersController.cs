using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spec_orders_poc.Orders;

namespace Spec_orders_poc.Controllers;

[ApiController]
[Authorize(Policy = "SupportOnly")]
[Route("api/customers/{id}/orders")]
public class CustomerOrdersController : ControllerBase
{
    private const int MaxExportRows = 10_000;

    private readonly OrderQueryService _orders;
    private readonly ILogger<CustomerOrdersController> _logger;

    public CustomerOrdersController(OrderQueryService orders, ILogger<CustomerOrdersController> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        string id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string status = "All",
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var query = new OrderQuery(id, from, to, status, page);

        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value)));
        }

        if (!await _orders.CustomerExistsAsync(id, ct))
        {
            return Problem(title: "Customer not found", detail: $"No customer with ID '{id}'.", statusCode: StatusCodes.Status404NotFound);
        }

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = await _orders.SearchAsync(query, ct);

        _logger.LogInformation(
            "Order search CustomerId={CustomerId} From={From} To={To} Status={Status} Page={Page} Rows={Rows} TotalCount={TotalCount} DurationMs={DurationMs}",
            id, from, to, status, page, result.Items.Count, result.TotalCount,
            System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string status = "All",
        CancellationToken ct = default)
    {
        var query = new OrderQuery(id, from, to, status);

        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value)));
        }

        if (!await _orders.CustomerExistsAsync(id, ct))
        {
            return Problem(title: "Customer not found", detail: $"No customer with ID '{id}'.", statusCode: StatusCodes.Status404NotFound);
        }

        var count = await _orders.CountAsync(query, ct);
        if (count > MaxExportRows)
        {
            _logger.LogWarning(
                "Order export rejected CustomerId={CustomerId} From={From} To={To} Status={Status} MatchCount={Count} Max={Max}",
                id, from, to, status, count, MaxExportRows);

            return Problem(
                title: "Too many orders to export",
                detail: $"{count} orders match; the maximum is {MaxExportRows}. Narrow the filters.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var safeId = new string(id.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=\"orders-{safeId}-{DateTime.UtcNow:yyyyMMdd}.csv\"";

        var exportStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        var rows = 0;

        try
        {
            var writer = new CsvOrderWriter(Response.Body);
            await writer.WriteHeaderAsync(ct);

            await foreach (var order in _orders.StreamAsync(query, ct))
            {
                await writer.WriteRowAsync(order, ct);
                rows++;
            }

            _logger.LogInformation(
                "Order export CustomerId={CustomerId} From={From} To={To} Status={Status} Rows={Rows} DurationMs={DurationMs}",
                id, from, to, status, rows,
                System.Diagnostics.Stopwatch.GetElapsedTime(exportStarted).TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Order export failed CustomerId={CustomerId} RowsWritten={Rows}", id, rows);

            if (!Response.HasStarted)
            {
                throw;
            }

            HttpContext.Abort();
        }

        return new EmptyResult();
    }
}
