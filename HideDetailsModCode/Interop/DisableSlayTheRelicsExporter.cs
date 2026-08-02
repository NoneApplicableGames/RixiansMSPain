using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace HideDetailsMod.HideDetailsModCode.Interop;

[HarmonyPatch]
static class DisableSlayTheRelicsExporter
{
    // Harmony executes this first to find what method to patch
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var StateExporter = AccessTools.TypeByName("SlayTheRelicsExporter.StateExporter");
        if (StateExporter == null) return [];
        MainFile.Logger.Info("SlayTheRelicsExporter.StateExporter Found! Blocking execution of ExportDeck, ExportPile, and PopulateCardMeta");
        return new MethodInfo?[] {
            StateExporter.GetMethod("ExportDeck"),
            StateExporter.GetMethod("ExportPile"),
            StateExporter.GetMethod("PopulateCardMeta"),
        }.OfType<MethodInfo>();
    }

    // Disable SlayTheRelics deck exporting
    // Returning false skips the original void method
    public static bool Prefix() => false;
}
