using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Collections.Generic;
using System.Linq;
using MySts1Mod.Encounters;
using Sts1Content; 

namespace MySts1Mod.Patches;

[HarmonyPatch(typeof(Glory), nameof(Glory.GenerateAllEncounters))]
public static class ActMonsterPoolPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        var list = __result.ToList();
        var heartBoss = ModelDb.Encounter<CorruptHeartBossEncounter>();
        var archBoss = ModelDb.Encounter<TheArchitectBossEncounter>();
        
        if (heartBoss != null && !list.Contains(heartBoss)) list.Add(heartBoss);
        if (archBoss != null && !list.Contains(archBoss)) list.Add(archBoss);

        __result = list;
    }
}