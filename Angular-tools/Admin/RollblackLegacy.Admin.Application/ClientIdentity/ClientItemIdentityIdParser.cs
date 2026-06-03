using RollblackLegacy.Admin.Application.Exceptions;

namespace RollblackLegacy.Admin.Application.ClientIdentity;

public static class ClientItemIdentityIdParser
{
    public static IReadOnlyList<int> Parse(string? rawIds, string fieldName = "ids")
    {
        if (string.IsNullOrWhiteSpace(rawIds))
        {
            throw new AdminValidationException(
                "No item ids were provided for the client identity audit.",
                new Dictionary<string, string[]>
                {
                    [fieldName] = ["Use a comma-separated list, for example 7754,12616,12617,39."]
                });
        }

        var tokens = rawIds
            .Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            throw new AdminValidationException(
                "No item ids were provided for the client identity audit.",
                new Dictionary<string, string[]>
                {
                    [fieldName] = ["Provide at least one positive item id."]
                });
        }

        var values = new List<int>(tokens.Length);
        var invalidTokens = new List<string>();

        foreach (var token in tokens)
        {
            if (int.TryParse(token, out var value))
            {
                values.Add(value);
                continue;
            }

            invalidTokens.Add(token);
        }

        if (invalidTokens.Count > 0)
        {
            throw new AdminValidationException(
                "One or more item ids could not be parsed.",
                new Dictionary<string, string[]>
                {
                    [fieldName] = [$"Invalid values: {string.Join(", ", invalidTokens)}."]
                });
        }

        EnsureWithinBatchLimit(values, fieldName);
        EnsurePositiveIds(values, fieldName);

        return values.Distinct().ToArray();
    }

    public static void EnsureWithinBatchLimit(IReadOnlyList<int> itemIds, string fieldName = "ids")
    {
        if (itemIds.Count <= ClientItemIdentityBatchLimits.MaxItemIdsPerRequest)
        {
            return;
        }

        throw new AdminValidationException(
            $"The client identity batch limit is {ClientItemIdentityBatchLimits.MaxItemIdsPerRequest} item ids per request.",
            new Dictionary<string, string[]>
            {
                [fieldName] =
                [
                    $"Received {itemIds.Count} ids. Split the request into smaller batches."
                ]
            },
            statusCode: 422);
    }

    private static void EnsurePositiveIds(IReadOnlyList<int> itemIds, string fieldName)
    {
        var invalidIds = itemIds.Where(x => x <= 0).Distinct().ToArray();
        if (invalidIds.Length == 0)
        {
            return;
        }

        throw new AdminValidationException(
            "One or more requested item ids are invalid.",
            new Dictionary<string, string[]>
            {
                [fieldName] = [$"All item ids must be greater than zero. Invalid values: {string.Join(", ", invalidIds)}."]
            });
    }
}
