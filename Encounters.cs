using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;
using MySts1Mod.Monsters;
using MegaCrit.Sts2.Core.Models.Monsters; 

namespace MySts1Mod.Encounters;


public sealed class TheArchitectBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override string BossNodePath => "res://animations/map/architect/architect_boss_node";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<TheArchitect>(), 
        ModelDb.Monster<Zapbot>(), ModelDb.Monster<Stabbot>() 
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
    
    
    public override string BossNodePath => "res://animations/map/DonuDeca/DonuDeca";

 
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> 
    { 
        ModelDb.Monster<Donu>(), 
        ModelDb.Monster<Deca>() 
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
       
        return new List<(MonsterModel, string?)> 
        { 
            (ModelDb.Monster<Donu>().ToMutable(), null),
            (ModelDb.Monster<Deca>().ToMutable(), null) 
        };
    }

    public override float GetCameraScaling() => 0.85f;
}