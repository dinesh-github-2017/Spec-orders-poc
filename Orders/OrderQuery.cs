namespace Spec_orders_poc.Orders;

public record OrderQuery(
    string CustomerId,
    DateOnly? From = null,
    DateOnly? To = null,
    string Status = "All",
    int Page = 1)
{
    public const int PageSize = 20;

    public static readonly string[] AllowedStatuses = ["All", "Pending", "Shipped", "Delivered", "Cancelled"];

    public IReadOnlyDictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            errors["id"] = ["Customer ID is required."];
        }

        if (From is not null && To is not null && From > To)
        {
            errors["from"] = ["'from' must be on or before 'to'."];
        }

        if (!AllowedStatuses.Contains(Status, StringComparer.Ordinal))
        {
            errors["status"] = [$"Status must be one of: {string.Join(", ", AllowedStatuses)}."];
        }

        if (Page < 1)
        {
            errors["page"] = ["Page must be 1 or greater."];
        }

        return errors;
    }
}
