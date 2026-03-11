using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;
using MySts1Mod.Monsters;
using MegaCrit.Sts2.Core.Models.Monsters; 

namespace MySts1Mod.Encounters;

// 1. 终焉建筑师 (劫持版)
public sealed class TheArchitectBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override string BossNodePath => "res://animations/map/architect/architect_boss_node";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<TheArchitect>(), // 假设你已定义 TheArchitect 类
        ModelDb.Monster<Zapbot>(), ModelDb.Monster<Stabbot>() // 预加载小怪
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<TheArchitect>().ToMutable(), null) };
    }
}

// 2. 腐化之心
public sealed class CorruptHeartBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override string BossNodePath => "res://mods/Sts1Content/animations/map/heart/heart_boss_node";
    
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<CorruptHeart>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<CorruptHeart>().ToMutable(), null) };
    }
}

// 3. 时光吞噬者
public sealed class TimeEaterBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    // 建议先借用女王图标直到你的资源包就绪
    public override string BossNodePath => "res://animations/map/time_eater/time_eater";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<TimeEater>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<TimeEater>().ToMutable(), null) };
    }
}

// 4. 觉醒者 (带2只虔诚雕刻家)
public sealed class AwakenedOneBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override bool IsWeak => false;

    // 【关键修复 1】必须设为 false，除非你为这场战斗制作了专门的 .tscn 背景
    public override bool HasScene => false;

    public override string BossNodePath => "res://animations/map/AwakenedOne/AwakenedOne";
    public override string CustomBgm => "event:/music/act3_boss_awakened_one"; 

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<DevotedSculptor>(),
        ModelDb.Monster<AwakenedOne>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 【关键修复 2】插槽名全部设为 null
        // 这样引擎会把 3 个怪全部加载出来（虽然初始会叠在一起）
        return new List<(MonsterModel, string?)> 
        { 
            (ModelDb.Monster<DevotedSculptor>().ToMutable(), null), // 小弟 1
            (ModelDb.Monster<AwakenedOne>().ToMutable(), null),    // BOSS
            (ModelDb.Monster<DevotedSculptor>().ToMutable(), null)  // 小弟 2
        };
    }

    public override float GetCameraScaling() => 0.85f;
}
public sealed class DonuDecaBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    
    // 手动指定地图图标路径
    public override string BossNodePath => "res://animations/map/DonuDeca/DonuDeca";

    // 预加载两个 Boss 的模型
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<Donu>(), 
        ModelDb.Monster<Deca>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 全部传 null，依靠怪物类内部的 FixPositions 来拉开距离
        return new List<(MonsterModel, string?)> 
        { 
            (ModelDb.Monster<Donu>().ToMutable(), null),
            (ModelDb.Monster<Deca>().ToMutable(), null) 
        };
    }

    public override float GetCameraScaling() => 0.85f;
}