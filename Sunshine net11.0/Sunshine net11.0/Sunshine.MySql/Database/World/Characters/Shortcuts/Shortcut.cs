using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Shortcuts
{
    public abstract class Shortcut
    {
        [Key]
        public int Id { get; set; }

        public abstract Protocol.Types.Shortcut GetNetworkShortcut();
    }
}
