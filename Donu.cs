using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;

using Sts1Content;

namespace MySts1Mod.Monsters;

public sealed class Donu : MonsterModel
{
    private bool _isAttackingNext = false; 

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 270);
    public override int MaxInitialHp => MinInitialHp;

    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private const int BeamCount = 2;
    private const int StrengthGain = 4;

    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public override void SetupSkins(NCreatureVisuals visuals)
    {
        foreach (Node child in visuals.GetChildren())
        {
            if (child is CanvasItem ci && child.Name != "DonuStaticSprite")
                ci.Visible = false;
        }

        Sprite2D sprite = visuals.GetNodeOrNull<Sprite2D>("DonuStaticSprite");
        if (sprite == null)
        {
            sprite = new Sprite2D();
            sprite.Name = "DonuStaticSprite";
            visuals.AddChild(sprite);
        }

        string imgPath = "res://images/monsters/deca/donu_static.png";
        var tex = GD.Load<Texture2D>(imgPath);
        if (tex != null)
        {
            sprite.Texture = tex;
            sprite.Scale = new Vector2(2.0f, 2.0f);
            sprite.Position = new Vector2(0, -150);
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
            if (node != null) node.Position = new Vector2(250, 200); 
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var list = new List<MonsterState>();
        var circleOfProtection = new MoveState("CIRCLE_OF_PROTECTION", CircleMove, new BuffIntent());
        var beam = new MoveState("BEAM", BeamMove, new MultiAttackIntent(BeamDamage, BeamCount));

        ConditionalBranchState ai = new ConditionalBranchState("DONU_AI");
        ai.AddState(beam, () => _isAttackingNext);
        ai.AddState(circleOfProtection, () => !_isAttackingNext);

        circleOfProtection.FollowUpState = ai;
        beam.FollowUpState = ai;

        list.AddRange(new MonsterState[] { circleOfProtection, beam, ai });
        return new MonsterMoveStateMachine(list, circleOfProtection);
    }

    private async Task CircleMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(this.Creature, "Buff", 0.5f);
        
        // 【核心修复】使用 Enemies 获取实时战斗中的所有敌人
        foreach (var m in base.CombatState.Enemies)
        {
            await PowerCmd.Apply<StrengthPower>(m, (decimal)StrengthGain, this.Creature, null);
        }

        _isAttackingNext = true; 
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BeamDamage).WithHitCount(BeamCount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.1f).Execute(null);
        
        _isAttackingNext = false; 
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) => new CreatureAnimator(new AnimState("idle_loop", true), controller);
}