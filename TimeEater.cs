using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities; 

using Sts1Content;
using MySts1Mod.Powers; 

namespace MySts1Mod.Monsters;

public sealed class TimeEater : MonsterModel
{
    private bool _usedHaste = false;
    private bool _firstTurn = true;

    public override LocString Title => new LocString("monsters", "TIME_EATER.name");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);
    public override int MaxInitialHp => MinInitialHp;

    private int ReverbDmg => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int HeadSlamDmg => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 26);

    // 【核心】借用原版建筑师的场景
    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public override void SetupSkins(NCreatureVisuals visuals)
    {
        // 1. 隐藏原有的所有建筑师节点
        foreach (Node child in visuals.GetChildren())
        {
            if (child is CanvasItem ci && child.Name != "TimeEaterSprite")
            {
                ci.Visible = false;
            }
        }

        // 2. 加载并挂载老头静态贴图
        if (visuals.GetNodeOrNull<Sprite2D>("TimeEaterSprite") == null)
        {
            Sprite2D teSprite = new Sprite2D();
            teSprite.Name = "TimeEaterSprite";
      
            // 注意：这里要匹配你 PCK 里的路径和文件名（根据你之前的截图，文件名首字母大写）
            string imgPath = "res://images/monsters/time_eater.png";
            var tex = GD.Load<Texture2D>(imgPath);

            if (tex != null)
            {
                teSprite.Texture = tex;
                teSprite.Scale = new Vector2(3.0f, 3.0f); // 放大 3 倍，可自行调整
                teSprite.Position = new Vector2(0, -200); // 向上偏移对齐
                visuals.AddChild(teSprite);
            }
            else
            {
                MainFile.Logger.Error($"[VFX ERROR] 找不到老头图片: {imgPath}");
            }
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        int initialWarp = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 4);
        await PowerCmd.Apply<TimeWarpPower>(this.Creature, (decimal)initialWarp, this.Creature, null);
        
        await Cmd.Wait(0.1f);
        FixPositions();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var list = new List<MonsterState>();
        var reverb = new MoveState("REVERBERATE", ReverbMove, new MultiAttackIntent(ReverbDmg, 3));
        var ripple = new MoveState("RIPPLE", RippleMove, new BuffIntent(), new DebuffIntent());
        var headSlam = new MoveState("HEAD_SLAM", HeadSlamMove, new SingleAttackIntent(HeadSlamDmg), new DebuffIntent());
        var haste = new MoveState("HASTE", HasteMove, new BuffIntent());

        ConditionalBranchState ai = new ConditionalBranchState("MAIN_AI");
        ai.AddState(haste, () => (decimal)base.Creature.CurrentHp < (decimal)base.Creature.MaxHp * 0.5m && !_usedHaste);
        
        RandomBranchState rnd = new RandomBranchState("RANDOM_MOVE");
        rnd.AddBranch(reverb, 45); 
        rnd.AddBranch(headSlam, 35); 
        rnd.AddBranch(ripple, 20);
        ai.AddState(rnd, () => true);

        reverb.FollowUpState = ai; ripple.FollowUpState = ai; headSlam.FollowUpState = ai; haste.FollowUpState = ai;
        list.AddRange(new MonsterState[] { reverb, ripple, headSlam, haste, ai, rnd });
        return new MonsterMoveStateMachine(list, reverb);
    }

    private async Task ReverbMove(IReadOnlyList<Creature> targets)
    {
        if (_firstTurn) { 
            TalkCmd.Play(new LocString("monsters", "TIME_EATER.dialog.0"), base.Creature, 3.0);
            _firstTurn = false; 
        }
        await DamageCmd.Attack(ReverbDmg).WithHitCount(3).FromMonster(this).Execute(null);
    }

    private async Task RippleMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<PlatingPower>(base.Creature, 20m, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(targets, 3m, this.Creature, null);
        await PowerCmd.Apply<WeakPower>(targets, 3m, this.Creature, null);
        if (AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies))
            await PowerCmd.Apply<FrailPower>(targets, 3m, this.Creature, null);
    }

    private async Task HeadSlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(HeadSlamDmg).FromMonster(this).Execute(null);
        await PowerCmd.Apply<MindRotPower>(targets, 1m, this.Creature, null);

        if(targets[0].Player is Player p) {
            for(int i = 0; i < 2; i++)
                await CardPileCmd.AddGeneratedCardToCombat(base.CombatState.CreateCard<Slimed>(p), PileType.Draw, false, CardPilePosition.Top);
        }
    }

    private async Task HasteMove(IReadOnlyList<Creature> targets)
    {
        _usedHaste = true;
        TalkCmd.Play(new LocString("monsters", "TIME_EATER.dialog.1"), base.Creature, 3.0);
        
        var debuffs = base.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        foreach (var p in debuffs) await PowerCmd.Remove(p);
        
        decimal targetHp = (decimal)base.Creature.MaxHp * 0.75m;
        decimal healAmt = targetHp - (decimal)base.Creature.CurrentHp;
        if (healAmt > 0) await CreatureCmd.Heal(base.Creature, healAmt, false);
        
        await CreatureCmd.GainBlock(base.Creature, 32m, ValueProp.Unpowered, null);
    }

    private void FixPositions() {
        var node = NCombatRoom.Instance?.GetCreatureNode(this.Creature);
        if (node != null) node.Position = new Vector2(600, 250); 
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) {
        // 借用建筑师动画防止崩溃
        AnimState idle = new AnimState("idle_loop", true);
        AnimState attack = new AnimState("attack");
        attack.NextState = idle;
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Attack", attack);
        return animator;
    }
}