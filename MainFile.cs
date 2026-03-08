using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts1Content; 

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "Sts1Content"; 
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
       
        // 游戏会自动发现并注册 CorruptHeart, BeatOfDeathPower 等。

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        Logger.Info("leddele Mod 补丁已通过自动扫描机制加载。");
    }
}