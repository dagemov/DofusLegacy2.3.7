namespace RollblackLegacy.Admin.Application.Exceptions;

public sealed class AdminValidationException : Exception
{
    public AdminValidationException(string message, IReadOnlyDictionary<string, string[]> errors, int statusCode = 400)
        : base(message)
    {
        Errors = errors;
        StatusCode = statusCode;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public int StatusCode { get; }
}
