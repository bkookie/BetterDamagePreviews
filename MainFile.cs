using BetterDamagePreviews.PreviewInitilizers;
using BetterDamagePreviews.PreviewSources;
using BetterDamagePreviews.PreviewVars;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

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

        PreviewManager.BeforeAttackInitialization.Add(new BaseCardDamagePreviewInitializer());
        PreviewManager.AfterAttackListeners.Add(new TeslaCoilDamagePreviewSource());

        RunManager.Instance.RoomEntered += ClearPreviewVarLookup;
        RunManager.Instance.RoomExited += ClearPreviewVarLookup;
    }

    private static void ClearPreviewVarLookup()
    {
        PreviewManager.AutoPreviewDamageLookup.Clear();
        PreviewManager.AutoPreviewHitCountLookup.Clear();
    }
}
