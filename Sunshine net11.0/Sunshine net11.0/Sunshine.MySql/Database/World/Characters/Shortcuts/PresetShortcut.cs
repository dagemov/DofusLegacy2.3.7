using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Shortcuts
{
    [Table("characters_shortcuts_items_presets")]
    public class PresetShortcut : Shortcut
    {
        public int OwnerId { get; set; }
        public int PresetId { get; set; }
        public int Slot { get; set; }

        public override Protocol.Types.Shortcut GetNetworkShortcut()
        {
            return new Protocol.Types.ShortcutObjectPreset(this.Slot, (sbyte)this.PresetId);
        }
    }
}
