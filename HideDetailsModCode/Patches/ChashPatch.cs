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
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace HideDetailsMod.HideDetailsModCode.Patches;

[HarmonyPatch(typeof(Clash), "OnPlay", MethodType.Async)]

static class ClashPatch
{
    static public ICardImgFactory AltArt = new CardImgFactory2<Clash>("event/clash_playable", card => card.CardIsPlayable());
    static internal bool? CardIsPlayable(this CardModel card)
    {
        if (card.IsCanonical) return false;
        return (bool?)IsPlayableMethod.Invoke(card, []);
    }

    static internal MethodInfo IsPlayableMethod => AccessTools.PropertyGetter(typeof(Clash), "IsPlayable");

    public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        instructions = AsyncMethodCall.Create(
            generator, instructions, original,
            callMethod: AccessTools.Method(typeof(ClashPatch), nameof(DoGrandFinaleVfx)),
            beforeState: original // run first
        );
        var execute = AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute));
        // return instructions;
        return new CodeMatcher(instructions, generator)
            .MatchStartForward(CodeMatch.Calls(execute))
            .ThrowIfInvalid("Could not locate the final .Execute call inside the fluent builder.")
            .InsertAndAdvance(CodeInstruction.Call(typeof(ClashPatch), nameof(Build)))
            .Instructions();
    }


    static AttackCommand Build(Clash _, AttackCommand command) => command
                .WithHitVfxNode(NGrandFinaleImpactVfx.Create)
                // .WithHitFx(vfx: "vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3")
                // .WithHitFx(tmpSfx: "blunt_attack.mp3")
                ;
    internal static async Task DoGrandFinaleVfx(Clash __instance)
    {
        NGrandFinaleVfx? nGrandFinaleVfx = NGrandFinaleVfx.Create(__instance.Owner.Creature);
        if (nGrandFinaleVfx != null)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGrandFinaleVfx);
            await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
        }
    }
}