using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players; 
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;

using MegaCrit.Sts2.Core.Entities.Cards; 

using Sts1Content;

namespace MySts1Mod.Monsters;

public sealed class Deca : MonsterModel
{
    private bool _isAttackingNext = true; 

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 270);
    public override int MaxInitialHp => MinInitialHp;

    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private const int BeamCount = 2;
    private const int ProtectBlock = 16;

    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public override void SetupSkins(MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals visuals)
    {
        foreach (Node child in visuals.GetChildren())
        {
            if (child is CanvasItem ci && child.Name != "DecaStaticSprite")
                ci.Visible = false;
        }

        Sprite2D sprite = visuals.GetNodeOrNull<Sprite2D>("DecaStaticSprite");
        if (sprite == null) {
            sprite = new Sprite2D(); sprite.Name = "DecaStaticSprite"; visuals.AddChild(sprite);
        }

        string imgPath = "res://images/monsters/deca/deca_static.png";
        var tex = GD.Load<Texture2D>(imgPath);
        if (tex != null) {
            sprite.Texture = tex; sprite.Scale = new Vector2(2.0f, 2.0f); sprite.Position = new Vector2(0, -150);
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        decimal artifactAmt = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5m, 4m);
        await PowerCmd.Apply<ArtifactPower>(this.Creature, artifactAmt, this.Creature, null);


        await Cmd.Wait(0.2f);
        var room = NCombatRoom.Instance;
        if (room != null) {
            var node = room.GetCreatureNode(this.Creature);
            if (node != null) node.Position = new Vector2(850, 200); 
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var list = new List<MonsterState>();
        var square = new MoveState("SQUARE_OF_PROTECTION", SquareMove, new DefendIntent());
        var beam = new MoveState("BEAM", BeamMove, new MultiAttackIntent(BeamDamage, BeamCount), new CardDebuffIntent());
        ConditionalBranchState ai = new ConditionalBranchState("DECA_AI");
        ai.AddState(beam, () => _isAttackingNext);
        ai.AddState(square, () => !_isAttackingNext);
        square.FollowUpState = ai;
        beam.FollowUpState = ai;
        list.AddRange(new MonsterState[] { square, beam, ai });
        return new MonsterMoveStateMachine(list, beam);
    }

    private async Task SquareMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(this.Creature, "Buff", 0.5f);
        
      
        foreach (var m in base.CombatState.Enemies)
        {
            await CreatureCmd.GainBlock(m, (decimal)ProtectBlock, ValueProp.Unpowered, null);
            if (AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies))
                await PowerCmd.Apply<PlatingPower>(m, 7m, this.Creature, null);
        }
        _isAttackingNext = true;
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BeamDamage).WithHitCount(BeamCount).FromMonster(this).Execute(null);

        foreach (var t in targets)
        {
            if (t.Player is Player player) 
            {
              
                await CardPileCmd.AddGeneratedCardToCombat(base.CombatState.CreateCard<Dazed>(player), PileType.Discard, false, CardPilePosition.Top);
                await CardPileCmd.AddGeneratedCardToCombat(base.CombatState.CreateCard<Dazed>(player), PileType.Discard, false, CardPilePosition.Top);
                 await CardPileCmd.AddGeneratedCardToCombat(base.CombatState.CreateCard<Dazed>(player), PileType.Discard, false, CardPilePosition.Top);
            }
        }
        _isAttackingNext = false;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) => new CreatureAnimator(new AnimState("idle_loop", true), controller);
}