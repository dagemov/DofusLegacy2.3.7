using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Maps.Houses;

namespace Sunshine.WorldServer.Game.Dialogs.Houses
{
    public enum HouseCodeDialogAction
    {
        UseCode = 0,
        ChangeCode = 1,
        UnlockChest = 2
    }

    public class HouseCodeDialog : IDialog
    {
        public Character Character { get; private set; }
        public House House { get; private set; }
        public bool IsChest { get; private set; }
        public int ElementId { get; private set; }
        public HouseCodeDialogAction Action { get; private set; }
        public bool ChangeCode => Action == HouseCodeDialogAction.ChangeCode;

        public HouseCodeDialog(Character character, House house, HouseCodeDialogAction action, bool isChest, int elementId = 0)
        {
            Character = character;
            House = house;
            Action = action;
            IsChest = isChest;
            ElementId = elementId > 0 ? elementId : house != null ? house.InteractiveId : 0;
        }

        public void Open()
        {
            Character.Dialog = this;
            Character.Client.Send(new LockableShowCodeDialogMessage(ChangeCode, (sbyte)(IsChest ? House.ChestCodeLength : House.HouseCodeLength)));
        }
    }
}
