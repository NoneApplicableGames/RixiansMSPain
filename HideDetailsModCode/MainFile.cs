using BaseLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;
using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using BaseLib.Extensions;
using System.Reflection;
using BaseLib.Audio;
using MegaCrit.Sts2.Core.Debug;
using System.Runtime.Loader;

namespace HideDetailsMod.HideDetailsModCode;

//You're recommended but not required to keep all your code in this package and all your assets in the HideDetailsMod folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "HideDetailsMod"; //At the moment, this is used only for the Logger and harmony names.
    public static Logger Logger { get; } = new(ModId, LogType.Generic);
    public static AutoModAudio Audio { get; } = new("res://HideDetailsMod/audio");
    public static bool DefectSetActive => false;
    public static bool IroncladSetActive => false;
    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var version = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "";
        var is107 = version.Contains(".107.");
        if (!is107)
        {
            string modFolder = Path.GetDirectoryName(assembly.Location);
            string betaPackPath = Path.Combine(modFolder, "HideDetailsMod.Beta.betapack");

            if (File.Exists(betaPackPath))
            {
                var asm = AssemblyLoadContext.GetLoadContext(typeof(ModManager).Assembly).LoadFromAssemblyPath(betaPackPath);
                AccessTools.Method(typeof(ModManager), "AssociateAssemblyWithMod").Invoke(null, [ModId, asm]);
                // ModManager.AssociateAssemblyWithMod(ModId, asm);
            }
        }

        MyModConfig.Init();
        CustomLocTableManager.Register("usernames");
        CustomLocTableManager.Register("artists");
        CustomLocTableManager.Register("event_chatter");
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);

        harmony.TryPatchAll(assembly);
    }

#if CANARY
    public static bool IsCanary => MyModConfig.EmulateCanaryMode;
#else
    public static bool IsCanary => false;
#endif
}