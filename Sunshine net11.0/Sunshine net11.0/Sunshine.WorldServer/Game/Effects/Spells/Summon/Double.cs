using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Summon
{
    [EffectHandler(EffectsEnum.Effect_Double)]
    public class Double : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Caster.IsAlive)
            {
                SummonedClone summonedClone = new SummonedClone(ActorManager.Instance.GenerateId(true), Caster, Caster.Stats.CloneAndChangeOwner(this), 
                    new ObjectPosition(Fight.Map, TargetedCell, Caster.Position.Direction));

                Caster.AddSummon(summonedClone);

                int indexSummoner = Fight.TimeLine.Fighters.IndexOf(Caster);
                Fight.TimeLine.Fighters.Insert(indexSummoner + 1, summonedClone);

                if (Caster.IsAttacker())
                    Fight.Team.AddAttacker(Caster, summonedClone);
                else
                    Fight.Team.AddDefender(Caster, summonedClone);

                ContextHandler.SendGameFightUpdateTeamMessage(Fight.Clients, Caster.Team);
                ContextHandler.SendGameFightTurnListMessage(Fight.Clients, Fight);
                ActionsHandler.SendGameActionFightSummonMessage(Fight.Clients, summonedClone);
                Fight.TriggerMarks(summonedClone.Position.Cell, summonedClone, TriggerTypeEnum.MOVE);
            }
        }
    }
}
