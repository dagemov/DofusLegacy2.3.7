using Rollback.Common.ORM.Config;

namespace Rollback.Admin.Configuration;

public sealed class RollbackAdminDatabasesOptions
{
    public DatabaseConfiguration Auth { get; set; } = new()
    {
        Host = "127.0.0.1",
        User = "root",
        Password = string.Empty,
        DatabaseName = "rollback_auth",
    };

    public DatabaseConfiguration World { get; set; } = new()
    {
        Host = "127.0.0.1",
        User = "root",
        Password = string.Empty,
        DatabaseName = "rollback_world",
    };
}
