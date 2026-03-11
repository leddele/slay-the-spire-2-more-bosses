using System.Threading.Tasks;
using BaseLib.Abstracts; 
using BaseLib.Extensions; 
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// 【核心修复】引入力量 Buff 所在的命名空间
using MegaCrit.Sts2.Core.Models.Powers; 

namespace MySts1Mod.Powers;

public sealed class CuriosityPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != null && cardPlay.Card.Type == CardType.Power)
        {
            this.Flash();
          
            await PowerCmd.Apply<StrengthPower>(base.Owner, (decimal)base.Amount, base.Owner, null);
        }
    }
}