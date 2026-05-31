namespace RollblackLegacy.Website.Infrastructure;

public static class HtmxRequestExtensions
{
    public static bool IsHtmxRequest(this HttpRequest request)
    {
        return string.Equals(request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
    }
}
