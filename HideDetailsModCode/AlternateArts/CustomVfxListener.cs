using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

// AllAbstractModelSubtypes
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllAbstractModelSubtypes), MethodType.Getter)]
static class RemoveListenerOnMainV107Patch
{
    [HarmonyPostfix]
    static internal void Postfix(ref Type[] __result)
    {
        var args = OS.GetCmdlineArgs();
        if (args.Contains("-noremovelistener")) return;
        var version = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "";
        var is107 = version.Contains(".107.");
        if (is107 || args.Contains("-removelistener"))
        {
            var list = __result.ToList();
            list.Remove(typeof(CustomVfxListener));
            __result = [.. list];
        }
    }
}

// TODO: make it general so i can apply the transforms to anything
// TODO: make work on main branch for multiplayer
class CustomVfxListener() : CustomSingletonModel(HookType.Combat)
{
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (MyModConfig.UseSimpleMode) return;

        if (cardSource is Squeeze squeeze)
        {
            var nCard = NCard.FindOnTable(squeeze);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlaySqueeze(.5f);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;
            if (SqueezeVfxs.ContainsKey(creature)) return;

            // Squeeze inward at the waist and stretch vertically
            NSqueezeVfx? vfx = NSqueezeVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.UntilRevert
            );
            if (vfx != null)
            {
                CreaturesWithVfx.Add(creature);
                SqueezeVfxs[creature] = vfx;
                await vfx.ApplyTask;
            }
        }
        if (cardSource is Flatten flatten)
        {
            var nCard = NCard.FindOnTable(flatten);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlayFlatten(.5f);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;
            if (FlattenVfxs.ContainsKey(creature)) return;

            // Squeeze inward at the waist and stretch vertically
            var vfx = NFlattenVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.UntilRevert
            );

            if (vfx != null)
            {
                CreaturesWithVfx.Add(creature);
                FlattenVfxs[creature] = vfx;
                await vfx.ApplyTask;
            }
        }
        if (cardSource is Rattle rattle)
        {
            var nCard = NCard.FindOnTable(rattle);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlayRattle(1f, 100);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;

            // Squeeze inward at the waist and stretch vertically
            var vfx = NRattleVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.Timed
            );
            if (vfx != null)
            {
                await (vfx.VfxTask ?? vfx.ApplyTask);
            }
        }
    }
    Dictionary<NCreature, NCreatureModifierVfx> FlattenVfxs { get; } = [];
    Dictionary<NCreature, NCreatureModifierVfx> SqueezeVfxs { get; } = [];
    HashSet<NCreature> CreaturesWithVfx { get; } = [];
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in CreaturesWithVfx)
        {
            NCreatureModifierVfx.ClearAll(creature.Visuals, animateRevert: true);
        }
        FlattenVfxs.Clear();
        SqueezeVfxs.Clear();
        return Task.CompletedTask;
    }

}