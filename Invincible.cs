using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.ValueProps;

namespace MySts1Mod.Powers;

public sealed class InvinciblePower : CustomPowerModel
{
    private class Data { public decimal damageReceivedThisTurn; }
    protected override object InitInternalData() => new Data();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";
    public override string CustomBigIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";

    public override int DisplayAmount => (int)Math.Max(0m, (decimal)base.Amount - GetInternalData<Data>().damageReceivedThisTurn);

    // 拦截伤害的钩子
    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return amount;
        
        decimal limit = (decimal)base.Amount;
        decimal received = GetInternalData<Data>().damageReceivedThisTurn;
        decimal remaining = Math.Max(0m, limit - received);

        if (amount > remaining)
        {
            this.Flash();
            return remaining;
        }
        return amount;
    }

    // 记录伤害
    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && !result.WasFullyBlocked)
        {
            GetInternalData<Data>().damageReceivedThisTurn += (decimal)result.UnblockedDamage;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    // 重置伤害计数
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            GetInternalData<Data>().damageReceivedThisTurn = 0m;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
}