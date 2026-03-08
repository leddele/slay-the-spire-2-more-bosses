using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;
using MySts1Mod.Monsters;
using MegaCrit.Sts2.Core.Models.Monsters; // 引用原版怪物

namespace MySts1Mod.Encounters;


// 1. 三层 BOSS：终焉建筑师 (The Architect)

public sealed class TheArchitectBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override string BossNodePath => "res://animations/map/architect/architect_boss_node";

    // 预加载列表
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<TheArchitect>(),
        // P1
        ModelDb.Monster<Zapbot>(), ModelDb.Monster<Stabbot>(), 
        ModelDb.Monster<Noisebot>(), ModelDb.Monster<Axebot>(),
        // P2 
        ModelDb.Monster<FrogKnight>(), ModelDb.Monster<MagiKnight>(), 
        ModelDb.Monster<MechaKnight>(), ModelDb.Monster<SpectralKnight>(), 
        ModelDb.Monster<FlailKnight>(),
        // P3 
        ModelDb.Monster<TheLost>(), ModelDb.Monster<TheForgotten>(),
        ModelDb.Monster<CubexConstruct>(), ModelDb.Monster<PunchConstruct>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<TheArchitect>().ToMutable(), null) };
    }
}


// 2. 三层 BOSS：腐化之心 (Corrupt Heart)

public sealed class CorruptHeartBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    // 地图图标路径
    public override string BossNodePath => "res://animations/map/heart/heart_boss_node";
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<CorruptHeart>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 单个 Boss 传 null 会自动居中
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<CorruptHeart>().ToMutable(), null) };
    }
}