using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts; 
using BaseLib.Extensions; 
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

// 引用入口文件所在的命名空间
using Sts1Content; 

namespace MySts1Mod.Powers;

public sealed class TimeWarpPower : CustomPowerModel
{
    // 1. 数据存储
    private class Data { 
        public int threshold = 12; 
    }
    protected override object InitInternalData() => new Data();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 2. 图标路径
    public override string CustomPackedIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";
    public override string CustomBigIconPath => "res://images/powers/" + Id.Entry.RemovePrefix().ToLowerInvariant() + ".png";

    // 3. 动态变量定义
    private class ThresholdVar : DynamicVar 
    {
        public ThresholdVar(string name) : base(name, 12m) { }
        protected override decimal GetBaseValueForIConvertible() 
        {
            if (base._owner is TimeWarpPower p)
            {
                return (decimal)p.GetInternalData<Data>().threshold;
            }
            return 12m;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> {
        new ThresholdVar("Threshold")
    };

    // 4. 核心逻辑
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        if (player != null)
        {
            this.Amount++; 
            var data = GetInternalData<Data>();

            if (this.Amount >= data.threshold)
            {
                this.Amount = 0; 
                this.Flash();

                // A. 强制结束回合
                PlayerCmd.EndTurn(player, false);

                // B. BOSS 强化 (4力量 + 15镀层)
                await PowerCmd.Apply<StrengthPower>(base.Owner, 4m, base.Owner, null);
                await PowerCmd.Apply<PlatingPower>(base.Owner, 10m, base.Owner, null);

                // C. 阈值递减 (最低 6)
                data.threshold = Math.Max(6, data.threshold - 1);

                // 【核心修复】DynamicVars 是一个 KeyValuePair 集合
                // pair.Key 是变量名，pair.Value 是具体的 DynamicVar 对象
                foreach (var pair in this.DynamicVars)
                {
                    if (pair.Key == "Threshold")
                    {
                        pair.Value.BaseValue = (decimal)data.threshold;
                        break;
                    }
                }
                
                MainFile.Logger.Info($"时间扭曲触发：新阈值 {data.threshold}");
            }
            
            // 刷新 UI 状态
            this.InvokeDisplayAmountChanged();
        }
    }
}