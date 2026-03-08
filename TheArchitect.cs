using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Ascension; 
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;        
using MegaCrit.Sts2.Core.Entities.Cards;        
using MegaCrit.Sts2.Core.Helpers;           
using MegaCrit.Sts2.Core.Models; 
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

using Sts1Content; 
using MySts1Mod.Powers; 
using StsVoidCard = MegaCrit.Sts2.Core.Models.Cards.Void; 

namespace MySts1Mod.Monsters;

public sealed class TheArchitect : MonsterModel
{
    private int _phase = 1; 
    private int _moveCount = 0; 
    private bool _isTransitioning = false; 

    // --- 基础数值 ---
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 1600, 1200);
    public override int MaxInitialHp => MinInitialHp;
    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public override void SetupSkins(NCreatureVisuals visuals)
    {
        visuals.GetNodeOrNull("SpineBody")?.Set("visible", true);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        int invAmount = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 300, 400);
        await PowerCmd.Apply<InvinciblePower>(this.Creature, (decimal)invAmount, this.Creature, null);
        
        await Cmd.Wait(0.1f);
        FixPositions();
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != this.Creature || _isTransitioning || this.MoveStateMachine == null) return;
        float hpPercent = (float)this.Creature.CurrentHp / (float)this.Creature.MaxHp;

        if (_phase == 1 && hpPercent <= 0.5f) TriggerPhaseTransition(2, "SUMMON_P2");
        else if (_phase == 2 && hpPercent <= 0.3f) TriggerPhaseTransition(3, "SUMMON_P3");
        await Task.CompletedTask;
    }

    private void TriggerPhaseTransition(int nextPhase, string moveId)
    {
        _phase = nextPhase; _moveCount = 0; _isTransitioning = true;
        var next = this.MoveStateMachine?.States.Values.OfType<MoveState>().FirstOrDefault(s => s.Id == moveId);
        if (next != null) this.SetMoveImmediate(next, true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var list = new List<MonsterState>();
        var stSummonP1 = new MoveState("SUMMON_P1", async (t) => await DoSummon(1), new BuffIntent());
        var stSummonP2 = new MoveState("SUMMON_P2", async (t) => await DoSummon(2), new BuffIntent());
        var stSummonP3 = new MoveState("SUMMON_P3", async (t) => await DoSummon(3), new BuffIntent());

        ConditionalBranchState nextMoveBranch = new ConditionalBranchState("NEXT_MOVE_BRANCH");
        var stSquad = new MoveState("SQUAD_ACTION", SquadAction, new BuffIntent());
        var stSolo = new MoveState("SOLO_ACTION", SoloAction, new SingleAttackIntent(50));

        nextMoveBranch.AddState(stSquad, HasTeam);
        nextMoveBranch.AddState(stSolo, () => !HasTeam());

        stSummonP1.FollowUpState = nextMoveBranch;
        stSummonP2.FollowUpState = nextMoveBranch;
        stSummonP3.FollowUpState = nextMoveBranch;
        stSquad.FollowUpState = nextMoveBranch;
        stSolo.FollowUpState = nextMoveBranch;

        list.AddRange(new MonsterState[] { stSummonP1, stSummonP2, stSummonP3, stSquad, stSolo, nextMoveBranch });
        return new MonsterMoveStateMachine(list, stSummonP1);
    }

    private bool HasTeam() => base.CombatState.GetTeammatesOf(this.Creature).Any(c => c != this.Creature && c.IsAlive);

    // --- 召唤行动 ---
    private async Task DoSummon(int phase)
    {
        _isTransitioning = true;
        await CreatureCmd.TriggerAnim(this.Creature, "Attack", 0.6f);
        
        List<Type> monsterTypes = phase switch {
            1 => new List<Type> { typeof(Zapbot), typeof(Stabbot), typeof(Noisebot), typeof(Axebot) },
            2 => new List<Type> { typeof(FrogKnight), typeof(MagiKnight), typeof(FlailKnight), typeof(MechaKnight), typeof(SpectralKnight) },
            _ => new List<Type> { typeof(TheLost), typeof(TheForgotten), typeof(PunchConstruct), typeof(CubexConstruct) }
        };

        foreach (var type in monsterTypes) {
            var model = ModelDb.GetById<MonsterModel>(ModelDb.GetId(type));
            if (model != null) {
                Creature minion = await CreatureCmd.Add(model.ToMutable(), base.CombatState, CombatSide.Enemy, null);
                await PowerCmd.Apply<MinionPower>(minion, 1m, this.Creature, null);
                await PowerCmd.Apply<PlatingPower>(minion, 15m, this.Creature, null); 
            }
        }
        await PowerCmd.Apply<SlipperyPower>(this.Creature, 999m, this.Creature, null);
        _isTransitioning = false; 
        await Cmd.Wait(0.2f);
        FixPositions();
    }

    // --- 团队模式 ---
    private async Task SquadAction(IReadOnlyList<Creature> targets)
    {
        int cycle = _moveCount % 3;
        await CreatureCmd.TriggerAnim(this.Creature, "Attack", 0.5f);

        if (cycle == 0) { // Buff
            foreach (var m in base.CombatState.GetTeammatesOf(this.Creature).Where(c => c != this.Creature && c.IsAlive)) {
                await PowerCmd.Apply<StrengthPower>(m, 5m, this.Creature, null);
                if (_phase >= 2) await PowerCmd.Apply<PlatingPower>(m, _phase == 3 ? 10m : 5m, this.Creature, null);
            }
        }
        else if (cycle == 1) { 
            if (_phase == 1) await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, 5, false);
            else if (_phase == 2) await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Draw, 4, false);
            else await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Draw, 5, false); 
            
            await CardPileCmd.AddToCombatAndPreview<StsVoidCard>(targets, PileType.Discard, _phase == 3 ? 3 : 2, false);
        }
        else { // 攻击
            int dmg = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies) ? (_phase switch { 1=>14, 2=>13, _=>12 }) : 10;
            await DamageCmd.Attack(dmg).WithHitCount(_phase + 2).FromMonster(this).WithAttackerAnim("Attack", 0.1f).Execute(null);
        }
        _moveCount++;
    }

    // --- 单人模式 ---
    private async Task SoloAction(IReadOnlyList<Creature> targets)
    {
        if (this.Creature.HasPower<SlipperyPower>()) {
            await CreatureCmd.TriggerAnim(this.Creature, "Attack", 0.5f);
            var slip = this.Creature.GetPower<SlipperyPower>();
            if (slip != null) await PowerCmd.Remove(slip); 

            if (_phase == 1) await PowerCmd.Apply<EnragePower>(this.Creature, (decimal)AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2), this.Creature, null);
            else if (_phase == 2) { await PowerCmd.Apply<ThornsPower>(this.Creature, 4m, this.Creature, null); await PowerCmd.Apply<PlatingPower>(this.Creature, 25m, this.Creature, null); }
            else {
                foreach (var t in targets) {
                    await PowerCmd.Apply<WeakPower>(t, 99m, this.Creature, null);
                    await PowerCmd.Apply<VulnerablePower>(t, 99m, this.Creature, null);
                    await PowerCmd.Apply<FrailPower>(t, 99m, this.Creature, null);
                }
            }
            _moveCount = 0; return;
        }

        await CreatureCmd.TriggerAnim(this.Creature, "Attack", 0.5f);

        if (_phase == 2) {
            int roll = new System.Random().Next(3);
            if (roll == 0) {
                await CreatureCmd.Add(ModelDb.Monster<TorchHeadAmalgam>().ToMutable(), base.CombatState, CombatSide.Enemy, null);
                await DamageCmd.Attack(20).WithHitCount(2).FromMonster(this).Execute(null);
                FixPositions();
            } else if (roll == 1) {
                await PowerCmd.Apply<ChainsOfBindingPower>(targets, 1m, this.Creature, null); 
                await PowerCmd.Apply<TenderPower>(targets, 1m, this.Creature, null);
                await PowerCmd.Apply<TangledPower>(targets, 1m, this.Creature, null);
            } else {
                await PowerCmd.Apply<StrengthPower>(this.Creature, (decimal)AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 4), this.Creature, null);
                await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, 5, false);
            }
        }
        else {
             int dmg = _phase == 1 ? 6 : 50;
             int hits = _phase == 1 ? 5 : 1;
             await DamageCmd.Attack(dmg).WithHitCount(hits).FromMonster(this).Execute(null);
        }
        _moveCount++;
    }

    private void FixPositions() {
        var room = NCombatRoom.Instance;
        if (room == null) return;
        var enemies = base.CombatState.GetTeammatesOf(this.Creature).ToList();
        if (!enemies.Contains(this.Creature)) enemies.Add(this.Creature);
        float startX = 50f; int idx = 0;
        foreach (var c in enemies) {
            var node = room.GetCreatureNode(c);
            if (node == null) continue;
            if (c == this.Creature) { node.Position = new Vector2(700f, 450f); node.ZIndex = 5; }
            else { node.Position = new Vector2(startX + (idx++ * 220f), 0f); }
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) {
        AnimState idle = new AnimState("idle_loop", true);
        AnimState attack = new AnimState("attack");
        attack.NextState = idle;
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", attack);
        animator.AddAnyState("Buff", attack);
        return animator;
    }
}