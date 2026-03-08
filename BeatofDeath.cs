using System.Threading.Tasks;
using BaseLib.Abstracts; 
using BaseLib.Extensions; 
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MySts1Mod.Powers;

public sealed class BeatOfDeathPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;


    public override string CustomPackedIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";
    public override string CustomBigIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        if (player != null)
        {
            this.Flash();
            var sourceMonster = base.Owner.Monster; 
            if (sourceMonster != null)
            {
                await DamageCmd.Attack(base.Amount).FromMonster(sourceMonster).Execute(context);
            }
            else
            {
                await DamageCmd.Attack(base.Amount).Execute(context);
            }
        }
    }
}