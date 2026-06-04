namespace RollblackLegacy.Admin.Application.Exceptions;

public sealed class AdminNotConfiguredException : Exception
{
    public AdminNotConfiguredException(string message)
        : base(message)
    {
    }
}
