using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace BetterDamagePreviews;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "BetterDamagePreviews"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
#if DEBUG
        Harmony.DEBUG = true;
#endif
        harmony.PatchAll();
    }
}
