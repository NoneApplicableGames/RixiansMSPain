using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace HideDetailsMod.HideDetailsModCode.Patches;

[HarmonyPatch]

static class ClashPatch
{
    static public ICardImgFactory AltArt = new CardImgFactory2<Clash>("event/clash_playable", card => card.CardIsPlayable());
    static internal bool? CardIsPlayable(this CardModel card)
    {
        if (card.IsCanonical) return false;
        return (bool?)IsPlayableMethod.Invoke(card, []);
    }

    static internal MethodInfo IsPlayableMethod => AccessTools.PropertyGetter(typeof(Clash), "IsPlayable");

    // [HarmonyTranspiler, HarmonyPatch(typeof(Clash), "OnPlay", MethodType.Async)]
    // public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    // {
    //     instructions = AsyncMethodCall.Create(
    //         generator, instructions, original,
    //         callMethod: AccessTools.Method(typeof(ClashPatch), nameof(DoGrandFinaleVfx)),
    //         beforeState: original // run first
    //     );
    //     instructions = AsyncMethodCall.Create(
    //       generator, instructions, original,
    //       callMethod: AccessTools.Method(typeof(ClashPatch), nameof(FinishPlay)),
    //       afterState: original // run first
    //   );
    //     return instructions;
    //     // var execute = AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute));
    //     // // return instructions;
    //     // return new CodeMatcher(instructions, generator)
    //     //     .MatchStartForward(CodeMatch.Calls(execute))
    //     //     .ThrowIfInvalid("Could not locate the final .Execute call inside the fluent builder.")
    //     //     .InsertAndAdvance(CodeInstruction.Call(typeof(ClashPatch), nameof(Build)))
    //     //     .Instructions();
    // }

    // private static Task FinishPlay()
    // {
    //     DoingEffect = false;
    //     return Task.CompletedTask;
    // }

    // static internal bool DoingEffect { get; set; } = false;

    // internal static async Task DoGrandFinaleVfx(Clash __instance)
    // {
    //     DoingEffect = true;
    //     NGrandFinaleVfx? nGrandFinaleVfx = NGrandFinaleVfx.Create(__instance.Owner.Creature);
    //     if (nGrandFinaleVfx != null)
    //     {
    //         NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGrandFinaleVfx);
    //         await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
    //     }
    // }

    // [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.WithHitFx))]
    // static class AttackCommandPatch
    // {
    //     [HarmonyPostfix]
    //     public static bool Prefix(AttackCommand __instance, ref AttackCommand __result)
    //     {
    //         if (!DoingEffect) return true;
    //         __result = __instance.WithHitFx(tmpSfx: "blunt_attack.mp3").WithHitVfxNode(NGrandFinaleImpactVfx.Create);
    //         DoingEffect = false;
    //         return false;
    //     }
    // }

    // static async Task OnPlay(this Clash card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    // {
    //     ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    //     await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).FromCard(card, cardPlay).Targeting(cardPlay.Target)
    //         .WithHitFx("vfx/vfx_attack_slash")
    //         .Execute(choiceContext);
    // }

    // TODO: try to get the above fully working.
    [HarmonyPatch(typeof(Clash), "OnPlay", MethodType.Async)]
    static internal bool Prefix(Clash __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        if (MyModConfig.ClashAsGrandFinale)
        {
            __result = OnPlay2(__instance, choiceContext, cardPlay);
            return false;
        }
        return true;
    }
    static async Task OnPlay2(this Clash card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NGrandFinaleVfx? nGrandFinaleVfx = NGrandFinaleVfx.Create(card.Owner.Creature);
        if (nGrandFinaleVfx != null)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGrandFinaleVfx);
            await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).FromCard(card, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3").WithHitVfxNode(NGrandFinaleImpactVfx.Create)
            .Execute(choiceContext);
    }
}