using Godot;
using System.Threading.Tasks;
using BaseLib.Abstracts; 
using BaseLib.Extensions; 
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MySts1Mod.Monsters;
using MegaCrit.Sts2.Core.Models; 

namespace MySts1Mod.Powers;

public sealed class UnawakenedPower : CustomPowerModel
{
    private class Data { public bool isReviving; }
    protected override object InitInternalData() => new Data();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";
    public override string CustomBigIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";

 
    public new Texture2D Icon {
        get {
            string path = ProjectSettings.GlobalizePath(CustomPackedIconPath);
            Image img = new Image();
            if (img.Load(path) == Error.Ok) return ImageTexture.CreateFromImage(img);
            return base.Icon;
        }
    }
    public new Texture2D BigIcon => Icon;

    // 1. 阻止战斗结算
    public override bool ShouldStopCombatFromEnding() => true;

    // 2. 阻止尸体消失
    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        if (creature != base.Owner) return true;
        return false; 
    }

    // 3. 正在复活时不可被选定
    public override bool ShouldAllowHitting(Creature creature)
    {
        if (creature != base.Owner) return true;
        return !GetInternalData<Data>().isReviving;
    }

    // 4. 强制伤害截断为 0
    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && GetInternalData<Data>().isReviving) return 0m;
        return amount;
    }

    // 5. 拦截死亡判定
    public override bool ShouldDie(Creature creature)
    {
        if (creature == base.Owner && creature.Monster is AwakenedOne boss)
        {
            if (boss.Phase == 1)
            {
                GetInternalData<Data>().isReviving = true; 
                this.Flash();
                // 启动异步重生流程
                _ = boss.TriggerFakeDeath(); 
                return false; 
            }
        }
        return true; 
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
}