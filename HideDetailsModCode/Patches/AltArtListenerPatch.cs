using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.Patches;

public static class CollectionExtensions
{
    public static void DoSafely<T>(this IEnumerable<T> sequence, object on, Action<T> action)
    {
        sequence.Do(item =>
        {
            try
            {
                action(item);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error thrown by {item} on {on}: {e}");
            }
        });
    }
}
[HarmonyPatch]
public class AltArtListenerPatch
{
    internal static SpireField<CardModel, Action?> NCardNeedsUpdateEvent { get; } = new(() => null);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.SubscribeToModel))]
    public static void NCardSubscribeToModel(NCard __instance, CardModel? model)
    { if (MyModConfig.UseCustomArt) if (model != null && __instance.IsInsideTree()) NCardNeedsUpdateEvent[model] += __instance.Reload; }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UnsubscribeFromModel))]
    public static void NCardUnsubscribeFromModel(NCard __instance, CardModel? model)
    { if (MyModConfig.UseCustomArt) if (model != null) NCardNeedsUpdateEvent[model] -= __instance.Reload; }

    //

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterPowerAmountChanged))]
    public static void AfterPowerAmountChanged(AbstractModel __instance, PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, power, amount), alt => alt.OnPowerApplied(__instance, choiceContext, power, amount)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
    public static void AfterCardPlayed(AbstractModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, cardPlay.Card), alt => alt.OnCardPlayed(__instance, choiceContext, cardPlay)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardExhausted))]
    public static void AfterCardExhausted(AbstractModel __instance, PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, card), alt => alt.OnCardExhausted(__instance, choiceContext, card, causedByEthereal)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardGeneratedForCombat))]
    public static void AfterCardGeneratedForCombat(AbstractModel __instance, CardModel card, Player? creator)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, card, creator), alt => alt.OnCardGenerated(__instance, card)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterDeath))]
    public static void AfterDeath(AbstractModel __instance, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, creature), alt => alt.OnDeath(__instance, choiceContext, creature, wasRemovalPrevented)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterSideTurnEnd))]
    public static void AfterSideTurnEnd(AbstractModel __instance, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, side), alt => alt.OnTurnEnd(__instance, choiceContext, side, participants)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterSideTurnStart))]
    public static void AfterSideTurnStart(AbstractModel __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, side), alt => alt.OnTurnStart(__instance, side, participants, combatState)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardDrawn))]
    public static void AfterCardDrawn(AbstractModel __instance, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, card, fromHandDraw), alt => alt.OnCardDrawn(__instance, choiceContext, card, fromHandDraw)); }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.EnchantInternal))]
    public static void EnchantInternal(CardModel __instance, EnchantmentModel enchantment, decimal amount)
    { if (MyModConfig.UseCustomArt) Arts.DoSafely(on: (__instance, enchantment, amount), alt => alt.OnCardEnchanted(__instance, enchantment, amount)); }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(CardModel), nameof(CardModel.ClearEnchantmentInternal))]
    // public static void ClearEnchantmentInternal(CardModel __instance)
    // { Arts.Do(alt => alt.OnEnchantmentCleared(__instance)); }
}