using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Actions
{
    public abstract class NpcAction : IDialog
    {
        public abstract void Execute();
    }
}
