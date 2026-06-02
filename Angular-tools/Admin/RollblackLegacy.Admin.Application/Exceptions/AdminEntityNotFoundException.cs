namespace RollblackLegacy.Admin.Application.Exceptions;

public sealed class AdminEntityNotFoundException : Exception
{
    public AdminEntityNotFoundException(string entityName, string entityKey)
        : base($"{entityName} '{entityKey}' was not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }

    public string EntityName { get; }

    public string EntityKey { get; }
}
