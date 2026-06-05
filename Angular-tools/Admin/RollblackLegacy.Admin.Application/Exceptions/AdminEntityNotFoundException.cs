namespace RollblackLegacy.Admin.Application.Exceptions;

public sealed class AdminEntityNotFoundException : Exception
{
    public AdminEntityNotFoundException(string entityName, string entityKey)
        : base($"No se encontro {entityName} '{entityKey}'.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }

    public string EntityName { get; }

    public string EntityKey { get; }
}
