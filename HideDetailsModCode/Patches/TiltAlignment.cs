using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Patches;

internal static class NCardExtensions
{
    static internal NotNullSpireField<NCard, NCardRotation> RotatedBy = new(() => new());
    internal class NCardRotation { public float Value { get; set; } = 0; }
    static internal void RotateBy(this NCard nCard, float degrees)
    {
        nCard.RotationDegrees += degrees;
        RotatedBy[nCard].Value += degrees;
    }
    static internal void ResetRotation(this NCard nCard)
    {
        nCard.RotationDegrees -= RotatedBy[nCard].Value;
        RotatedBy[nCard].Value = 0;
    }
    static internal void SetRotationTo(this NCard nCard, float degrees)
    {
        nCard.RotateBy(degrees - RotatedBy[nCard].Value);
    }
}
[HarmonyPatch]
public static class TiltAlignment
{
    internal static float AlignmentRotationDegrees => -15f;
    // public const float AlignmentRotationDegrees = -6.28f;

    // #region UpdateVisualsRegion
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    // internal static void UpdateVisuals(NCard __instance)
    // {
    //     if (!GodotObject.IsInstanceValid(__instance)) return;
    //     if (__instance.Model is not Alignment) return;
    //     // You spin me right 'round baby; right 'round!
    //     __instance.RotateBy(-AlignmentRotationDegrees);
    // }
    // #endregion

    // #region ModelChangedRegion
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(NCard), nameof(NCard._Ready))]
    // internal static void Ready(NCard __instance)
    // {
    //     if (!GodotObject.IsInstanceValid(__instance)) return;
    //     __instance.ModelChanged += OnModelChangedCache[__instance];
    // }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(NCard), nameof(NCard.OnReturnedFromPool))]
    // internal static void ReturnedFromPool(NCard __instance)
    // {
    //     if (!GodotObject.IsInstanceValid(__instance)) return;
    //     // ensure no dupes
    //     __instance.ModelChanged -= OnModelChangedCache[__instance];
    //     __instance.ModelChanged += OnModelChangedCache[__instance];
    // }
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    // internal static void FreedToPool(NCard __instance)
    // {
    //     if (!GodotObject.IsInstanceValid(__instance)) return;
    //     __instance.ModelChanged -= OnModelChangedCache[__instance];
    //     __instance.ResetRotation();
    // }
    // static internal NotNullSpireField<NCard, Action<CardModel?>> OnModelChangedCache { get; } = new((nCard) =>
    //     oldCard => OnModelChanged(nCard, oldCard, newCard: nCard.Model)
    // );
    // static internal void OnModelChanged(NCard nCard, CardModel? oldCard, CardModel? newCard)
    // {
    //     if (oldCard is Alignment && newCard is not Alignment) nCard.ResetRotation();
    //     else if (oldCard is not Alignment && newCard is Alignment) nCard.RotateBy(AlignmentRotationDegrees);
    // }
    // #endregion


    #region SubUnsubRegion
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.SubscribeToModel))]
    internal static void Subscribe(NCard __instance, CardModel? model)
    {
        if (MyModConfig.UseSimpleMode) return;

        if (!GodotObject.IsInstanceValid(__instance)) return;
        if (model != null && __instance.IsInsideTree() && model is Alignment)
            __instance.RotateBy(AlignmentRotationDegrees);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UnsubscribeFromModel))]
    public static void Unsubscribe(NCard __instance, CardModel? model)
    {
        if (MyModConfig.UseSimpleMode) return;

        if (!GodotObject.IsInstanceValid(__instance)) return;
        if (model != null && model is Alignment)
            __instance.ResetRotation();
    }
    #endregion
}