using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities; 

using Sts1Content;
using MySts1Mod.Powers; 
using StsVoidCard = MegaCrit.Sts2.Core.Models.Cards.Void; 

namespace MySts1Mod.Monsters;

public sealed class AwakenedOne : MonsterModel
{
    public int Phase = 1; 
    private bool _firstTurnP2 = true;


    private decimal _savedStrength = 0m;
    private decimal _regenAmount = 0m;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 360, 320);
    public override int MaxInitialHp => MinInitialHp;

    protected override string VisualsPath => "res://scenes/creature_visuals/architect.tscn";

    public async Task TriggerFakeDeath()
    {
      
        var strPower = this.Creature.GetPower<StrengthPower>();
        if (strPower != null)
        {
            _savedStrength = strPower.Amount;
        }

        Phase = 2;
        MainFile.Logger.Info($"[Awakened] 假死触发！保存的力量值为: {_savedStrength}");

        var rebirthMove = this.MoveStateMachine?.States.Values.OfType<MoveState>().FirstOrDefault(s => s.Id == "REBIRTH");
        if (rebirthMove != null) this.SetMoveImmediate(rebirthMove, true);
        
        await Task.CompletedTask;
    }

    public override void SetupSkins(NCreatureVisuals visuals)
    {
        foreach (Node child in visuals.GetChildren())
        {
            if (child is CanvasItem ci && child.Name != "AwakenedStaticSprite")
            {
                ci.Visible = false;
            }
        }
        UpdateVisuals(visuals);
    }

    private void UpdateVisuals(NCreatureVisuals visuals)
    {
        Sprite2D sprite = visuals.GetNodeOrNull<Sprite2D>("AwakenedStaticSprite");
        if (sprite == null)
        {
            sprite = new Sprite2D();
            sprite.Name = "AwakenedStaticSprite";
            visuals.AddChild(sprite);
        }
        string fileName = Phase == 1 ? "awakened_p1.png" : "awakened_p2.png";
        string imgPath = $"res://images/monsters/awakened/{fileName}";
        var tex = GD.Load<Texture2D>(imgPath);
        if (tex != null)
        {
            sprite.Texture = tex;
            sprite.Scale = Phase == 1 ? new Vector2(3.0f, 3.0f) : new Vector2(3.5f, 3.5f); 
            sprite.Position = new Vector2(0, -350); 
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        decimal curiosity = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4m, 2m);
        await PowerCmd.Apply<CuriosityPower>(this.Creature, curiosity, this.Creature, null);
        await PowerCmd.Apply<UnawakenedPower>(this.Creature, 1m, this.Creature, null);

     
        _regenAmount = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30m, 20m);
        await PowerCmd.Apply<RegenPower>(this.Creature, _regenAmount, this.Creature, null);

        await Cmd.Wait(0.2f);
        FixPositions();
    }

    private void FixPositions()
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;
        var enemies = base.CombatState.GetTeammatesOf(this.Creature).ToList();
        if (!enemies.Contains(this.Creature)) enemies.Add(this.Creature);

        float centerX = 500f; 
        float baseY = 250f;   
        float spacing = 400f; 

        var minions = enemies.Where(e => e.Monster is DevotedSculptor).ToList();
        var boss = enemies.FirstOrDefault(e => e.Monster is AwakenedOne);

        if (boss != null) {
            var node = room.GetCreatureNode(boss);
            if (node != null) 
            {
                node.Position = new Vector2(centerX, baseY);
                node.ZIndex = 0; 
            }
        }

        for (int i = 0; i < minions.Count; i++) {
            var node = room.GetCreatureNode(minions[i]);
            if (node == null) continue;
            
            float xPos = i == 0 ? centerX - spacing : centerX + spacing;
            node.Position = new Vector2(xPos, baseY-50f); 
            node.ZIndex = 10; 
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var list = new List<MonsterState>();
        var slash = new MoveState("SLASH", SlashMove, new SingleAttackIntent(25));
        var soulStrike = new MoveState("SOUL_STRIKE", SoulStrikeMove, new MultiAttackIntent(7, 4));
        var rebirth = new MoveState("REBIRTH", RebirthMove, new HealIntent(), new BuffIntent()) { MustPerformOnceBeforeTransitioning = true };
        var darkEcho = new MoveState("DARK_ECHO", DarkEchoMove, new SingleAttackIntent(48));
        var sludge = new MoveState("SLUDGE", SludgeMove, new SingleAttackIntent(24), new DebuffIntent());
        var tackle = new MoveState("TACKLE", TackleMove, new MultiAttackIntent(13, 3));

        ConditionalBranchState mainAI = new ConditionalBranchState("MAIN_AI");
        RandomBranchState rndP1 = new RandomBranchState("RND_P1");
        rndP1.AddBranch(slash, 1); rndP1.AddBranch(soulStrike, 1);
        RandomBranchState rndP2 = new RandomBranchState("RND_P2");
        rndP2.AddBranch(sludge, 1); rndP2.AddBranch(tackle, 1);

        mainAI.AddState(rndP1, () => Phase == 1);
        mainAI.AddState(rndP2, () => Phase == 2);

        slash.FollowUpState = mainAI; soulStrike.FollowUpState = mainAI;
        darkEcho.FollowUpState = mainAI; sludge.FollowUpState = mainAI;
        tackle.FollowUpState = mainAI;
        rebirth.FollowUpState = darkEcho;

        list.AddRange(new MonsterState[] { slash, soulStrike, rebirth, darkEcho, sludge, tackle, mainAI, rndP1, rndP2 });
        return new MonsterMoveStateMachine(list, slash);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets) => await DamageCmd.Attack(25).FromMonster(this).Execute(null);
    private async Task SoulStrikeMove(IReadOnlyList<Creature> targets) => await DamageCmd.Attack(7).WithHitCount(4).FromMonster(this).Execute(null);

    private async Task RebirthMove(IReadOnlyList<Creature> targets)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(this.Creature);
        if (node?.Visuals != null) UpdateVisuals(node.Visuals);
        
        await CreatureCmd.Heal(base.Creature, (decimal)this.MaxInitialHp);
        
     
        if (this.Creature.HasPower<UnawakenedPower>()) await PowerCmd.Remove(this.Creature.GetPower<UnawakenedPower>());
        if (this.Creature.HasPower<CuriosityPower>()) await PowerCmd.Remove(this.Creature.GetPower<CuriosityPower>());
        
        var debuffs = base.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        foreach (var p in debuffs) await PowerCmd.Remove(p);

    
        if (_savedStrength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(this.Creature, _savedStrength, this.Creature, null);
            MainFile.Logger.Info($"[Awakened] 二阶段补发保留的力量: {_savedStrength}");
        }
        
        if (_regenAmount > 0 && !this.Creature.HasPower<RegenPower>())
        {
            await PowerCmd.Apply<RegenPower>(this.Creature, _regenAmount, this.Creature, null);
        }

        _firstTurnP2 = false; 
    }

    private async Task DarkEchoMove(IReadOnlyList<Creature> targets) => await DamageCmd.Attack(48).FromMonster(this).Execute(null);
    private async Task SludgeMove(IReadOnlyList<Creature> targets) {
        await DamageCmd.Attack(24).FromMonster(this).Execute(null);
        foreach(var t in targets) if(t.Player is Player p) 
            await CardPileCmd.AddGeneratedCardToCombat(base.CombatState.CreateCard<StsVoidCard>(p), PileType.Draw, true, CardPilePosition.Random);
    }
    private async Task TackleMove(IReadOnlyList<Creature> targets) => await DamageCmd.Attack(13).WithHitCount(3).FromMonster(this).Execute(null);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) => new CreatureAnimator(new AnimState("idle_loop", true), controller);
}