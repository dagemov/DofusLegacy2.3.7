namespace RollblackLegacy.Admin.Application.Exceptions;

public sealed class AdminConflictException : Exception
{
    public AdminConflictException(string message)
        : base(message)
    {
    }
}
