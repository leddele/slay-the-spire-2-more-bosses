using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
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

        // 批量获取所有自定义 BOSS 遭遇
        var customBosses = new List<EncounterModel?> {
            ModelDb.Encounter<TheArchitectBossEncounter>(),
            ModelDb.Encounter<CorruptHeartBossEncounter>(),
            ModelDb.Encounter<TimeEaterBossEncounter>(),
            ModelDb.Encounter<AwakenedOneBossEncounter>(),
            ModelDb.Encounter<DonuDecaBossEncounter>()
        };

        foreach (var boss in customBosses)
        {
            if (boss != null && !list.Contains(boss))
            {
                list.Add(boss);
            }
        }

        __result = list;
    }
}