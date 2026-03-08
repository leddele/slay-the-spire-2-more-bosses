using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx; 
using MegaCrit.Sts2.Core.Nodes;            
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Sts1Content; 
using MySts1Mod.Powers; 
// 为虚空牌起个别名，防止和 System.Void 冲突
using StsVoidCard = MegaCrit.Sts2.Core.Models.Cards.Void; 

namespace MySts1Mod.Monsters;

public sealed class CorruptHeart : MonsterModel
{
    private int _buffCount = 0;

    // --- 数值配置 ---
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 900, 800);
    public override int MaxInitialHp => MinInitialHp;

    private int BloodShotsDamage => 2;
    private int BloodShotsCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 12);
    private int EchoDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 45, 40);

    // --- 视觉资源配置 ---
    // 这里指向一个占位场景，实际显示由 SetupSkins 控制
    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public override void SetupSkins(NCreatureVisuals visuals)
    {
        // 1. 隐藏原有的所有建筑师节点
        foreach (Node child in visuals.GetChildren())
        {
            if (child is CanvasItem ci && child.Name != "HeartStaticSprite")
            {
                ci.Visible = false;
            }
        }

        // 2. 加载并挂载心脏静态贴图
        if (visuals.GetNodeOrNull<Sprite2D>("HeartStaticSprite") == null)
        {
            Sprite2D heartSprite = new Sprite2D();
            heartSprite.Name = "HeartStaticSprite";
      
            string imgPath = "res://images/monsters/heart/heart_static.png";
            var tex = GD.Load<Texture2D>(imgPath);

            if (tex != null)
            {
                heartSprite.Texture = tex;
                heartSprite.Scale = new Vector2(2.8f, 2.8f); 
                heartSprite.Position = new Vector2(0, -400); 
                visuals.AddChild(heartSprite);
            }
            else
            {
                MainFile.Logger.Error($"[VFX ERROR] 找不到心脏图片: {imgPath}");
            }
        }
    }

    // --- 战斗初始化 ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        


        int invincibleCap = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 200, 300);
        int beatAmount = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
        

        await PowerCmd.Apply<InvinciblePower>(this.Creature, (decimal)invincibleCap, this.Creature, null);
        await PowerCmd.Apply<BeatOfDeathPower>(this.Creature, (decimal)beatAmount, this.Creature, null);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        // 定义状态
        var stDebilitate = new MoveState("DEBILITATE", DebilitateMove, new DebuffIntent(), new CardDebuffIntent());
        
        // 多段攻击
        var stMulti_A = new MoveState("MULTI_A", BloodShotsMove, new MultiAttackIntent(BloodShotsDamage, BloodShotsCount));
        var stMulti_B = new MoveState("MULTI_B", BloodShotsMove, new MultiAttackIntent(BloodShotsDamage, BloodShotsCount));
        
        // 重击
        var stHeavy_A = new MoveState("HEAVY_A", EchoAttackMove, new SingleAttackIntent(EchoDamage));
        var stHeavy_B = new MoveState("HEAVY_B", EchoAttackMove, new SingleAttackIntent(EchoDamage));
        
        // 强化
        var stBuff = new MoveState("BUFF", BuffLogicMove, new BuffIntent());

        // 循环逻辑：Debuff -> (随机 A 或 B) -> ...
        RandomBranchState stRandomCycle = new RandomBranchState("CYCLE_START");
        stRandomCycle.AddBranch(stMulti_A, 1);
        stRandomCycle.AddBranch(stHeavy_B, 1);

     
        stDebilitate.FollowUpState = stRandomCycle;

        
        stMulti_A.FollowUpState = stHeavy_A;
        stHeavy_A.FollowUpState = stBuff;

   
        stHeavy_B.FollowUpState = stMulti_B;
        stMulti_B.FollowUpState = stBuff;

    
        stBuff.FollowUpState = stRandomCycle;

        // 添加所有状态到列表
        list.Add(stDebilitate);
        list.Add(stRandomCycle);
        list.Add(stMulti_A);
        list.Add(stMulti_B);
        list.Add(stHeavy_A);
        list.Add(stHeavy_B);
        list.Add(stBuff);

        return new MonsterMoveStateMachine(list, stDebilitate);
    }


    private async Task DebilitateMove(IReadOnlyList<Creature> targets)
    {
        // 播放吼叫音效
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/the_insatiable/the_insatiable_scream");
        
   
        await CreatureCmd.TriggerAnim(this.Creature, "Cast", 0.5f);

        // 【特效】播放全屏声波/尖叫特效
        VfxCmd.PlayOnCreatureCenter(this.Creature, "vfx/vfx_scream");
        
        // 【特效】屏幕震动
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Long, 0f);

        // 施加 Debuff
        await PowerCmd.Apply<VulnerablePower>(targets, 99m, this.Creature, null);
        await PowerCmd.Apply<WeakPower>(targets, 99m, this.Creature, null);
        await PowerCmd.Apply<FrailPower>(targets, 99m, this.Creature, null);

        // 塞入状态牌
        foreach (Creature target in targets)
        {
            if (target.Player != null)
            {
                var combat = base.CombatState;
  
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<Dazed>(target.Player), PileType.Draw, false, CardPilePosition.Top);
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<Slimed>(target.Player), PileType.Draw, false, CardPilePosition.Top);
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<Wound>(target.Player), PileType.Draw, false, CardPilePosition.Top);
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<Burn>(target.Player), PileType.Draw, false, CardPilePosition.Top);
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<StsVoidCard>(target.Player), PileType.Draw, false, CardPilePosition.Top);
            }
        }
    }


    private async Task BloodShotsMove(IReadOnlyList<Creature> targets)
    {
     
        await DamageCmd.Attack(BloodShotsDamage)
            .WithHitCount(BloodShotsCount)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.1f) // 快速攻击动画
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/generic_multi_attack") // 多段攻击音效
            .WithHitFx("vfx/vfx_bite") 
            .Execute(null);
    }

   
    private async Task EchoAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(EchoDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.6f) // 慢速重击动画
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/generic_heavy_impact") // 重击音效
            .WithHitFx("vfx/vfx_heavy_blunt") // 【特效】巨大的钝击特效
            .AfterAttackerAnim(delegate
            {
                // 【特效】强烈的屏幕反馈
                NCombatRoom.Instance?.RadialBlur(VfxPosition.Center); // 径向模糊
                NGame.Instance?.DoHitStop(ShakeStrength.Strong, ShakeDuration.Normal); // 顿帧
                return Task.CompletedTask;
            })
            .Execute(null);
    }

    // 强化 
    private async Task BuffLogicMove(IReadOnlyList<Creature> targets)
    {
        // 播放通用的强化音效
      SfxCmd.Play("event:/sfx/enemy/enemy_attacks/the_insatiable/the_insatiable_scream");
        await CreatureCmd.TriggerAnim(this.Creature, "Buff", 0.5f);
        
   
     VfxCmd.PlayOnCreatureCenter(this.Creature, "vfx/vfx_scream");

        // 力量恢复逻辑 (如果被减少了力量，先补回来)
        var strPower = this.Creature.GetPower<StrengthPower>();
        int recovery = (strPower != null && strPower.Amount < 0) ? -Mathf.FloorToInt((float)strPower.Amount) : 0;
        await PowerCmd.Apply<StrengthPower>(this.Creature, (decimal)(2 + recovery), this.Creature, null);

        // 循环 Buff 逻辑
        switch (_buffCount)
        {
            case 0: 
                await PowerCmd.Apply<ArtifactPower>(this.Creature, 2m, this.Creature, null); 
                break;
            case 1: 
                await PowerCmd.Apply<BeatOfDeathPower>(this.Creature, 1m, this.Creature, null); 
                break; 
            case 2: 
                await PowerCmd.Apply<PainfulStabsPower>(this.Creature, 1m, this.Creature, null); 
                break;
            case 3: 
                await PowerCmd.Apply<StrengthPower>(this.Creature, 10m, this.Creature, null); 
                break;
            default: 
                await PowerCmd.Apply<StrengthPower>(this.Creature, 50m, this.Creature, null); 
                break;
        }
        _buffCount++;
    }

    // --- 动画控制器 (简单映射) ---
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState attack = new AnimState("attack");
        AnimState buff = new AnimState("buff");

     
        attack.NextState = idle;
        buff.NextState = idle;

        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        
   
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Buff", buff);
        animator.AddAnyState("Cast", buff);
        
        return animator;
    }
}