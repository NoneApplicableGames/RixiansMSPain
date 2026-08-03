using System.Reflection;
using BaseLib.Utils;
using BaseLib.Utils.ModInterop;
using Godot;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode;
using HideDetailsMod.HideDetailsModCode.Patches;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;

class ShowAllArtsOfCard : AbstractConsoleCmd
{
    public override string CmdName => "mspaincard";
    public override string Args => "<card-id:string>";
    public override string Description => "Shows all art variants of a card. Screaming snake case ('BODY_SLAM', not 'Body Slam').";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false, "No card name specified.");
        }
        string cardName = args[0].ToUpperInvariant();
        CardModel? cardModel = ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry == cardName);
        if (cardModel == null)
        {
            return new CmdResult(success: false, "Card '" + cardName + "' not found");
        }

        var variants = Compat.GenerateAllVersions(cardModel).ToList();
        if (variants.Count == 0)
        {
            return new(false, "Card '" + cardName + "' has no MSPain variants");
        }

        ActiveScreen?.Close();
        var inspectScreen = ActiveScreen ??= NInspectCardScreen.Create();

        NGame.Instance?.AddChildSafely(inspectScreen);
        if (inspectScreen == null)
            return new CmdResult(success: false, $"No inspection screen could be created");

        inspectScreen?.Open(variants, 0);

        return new CmdResult(success: true, $"Previewed all '{cardModel.Id.Entry}' arts");
    }
    static NInspectCardScreen? ActiveScreen;
    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            var candidates = Compat.GetAllCardsWithMSPainImages().Select(card => card.Id.Entry);
            return CompleteArgument(candidates, [], args.FirstOrDefault() ?? "");
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.SetCard))]
static class InspectScreenPatch

{
    [HarmonyPatch(nameof(NInspectCardScreen.Open))]
    [HarmonyPatch(nameof(NInspectCardScreen.SetCard))]
    static void Prefix(NInspectCardScreen __instance)
    {
        __instance._upgradeTickbox.Enable();
    }

    [HarmonyPatch(nameof(NInspectCardScreen.Open))]
    [HarmonyPatch(nameof(NInspectCardScreen.SetCard))]
    static void Postfix(NInspectCardScreen __instance)
    {
        var model = __instance._card.Model;
        if (model == null) return;
        if (Compat.Types.Get(model) is { } type)
        {
            switch (type)
            {
                case Compat.CustomImgType.OnlyBase:
                    __instance._upgradeTickbox.Disable();
                    __instance._upgradeTickbox.IsTicked = false;
                    break;
                case Compat.CustomImgType.OnlyUpgrade:
                    __instance._upgradeTickbox.Disable();
                    __instance._upgradeTickbox.IsTicked = true;
                    break;
                case Compat.CustomImgType.Both:
                    break;
            }
        }
    }
}

//public virtual string ID => model.Id.Entry;
[HarmonyPatch("STS2Export.Exporter.CardExport", "Id", MethodType.Getter)]
static class ExporterIdsPatch
{
    static internal void PostFix(ref string __result, CardModel ___model)
    {
        var Img = CardImg.Of(___model.PortraitPath);
        if (Img == null) return;
        var Canonical = new CardImg(___model);
        if (___model.IsUpgraded) Canonical = Canonical.Upgraded();
        if (Img == Canonical) return;
        var extra = Img.Path.GetTextAfter(Canonical.Path);
        if (extra.StartsWith('_')) extra = extra[1..];
        __result = __result + "-" + extra;
    }
}

[HarmonyPatch("STS2Export.Exporter.CardExport", "FindAll")]
static class Compat
{
    static readonly MethodInfo targetMethod = AccessTools.Method("STS2Export.Exporter.CardExport:TargetMethodName");
    static internal bool Prepare() => targetMethod != null;
    static internal bool PostFix(ref List<object> __result)
    {
        if (!MyModConfig.ShouldPatchCardExporter) return true;
        var All = ModelDb.AllCards
                    .Where(m => m.ShouldShowInCardLibrary)
                    .SelectMany(GenerateAllVersionsInternal)
                    .SelectMany<(CardModel card, CustomImgType type), Interop.CardExport>(m => m.type switch
                    {
                        CustomImgType.OnlyBase => [new Interop.CardExport(m.card, 0)],
                        CustomImgType.OnlyUpgrade => [new Interop.CardExport(m.card.Downgrade(), 1)],
                        CustomImgType.Both => [new Interop.CardExport(m.card, 0), new Interop.CardExport(m.card, 1)],
                        _ => throw new NotImplementedException()
                    }
                    );
        var existing = __result.ToList();

        __result = [.. All, .. existing.Where(v => v.GetType().Name == "MadScienceExport")];
        return false;
    }
    static CardModel Downgrade(this CardModel card)
    {
        card.DowngradeInternal();
        return card;
    }
    public enum CustomImgType { OnlyBase, OnlyUpgrade, Both }
    static public readonly SpireField<CardModel, CustomImgType?> Types = new SpireField<CardModel, CustomImgType?>(() => null).CopyOnClone();

    static public IEnumerable<CardModel> GetAllCardsWithMSPainImages()
    {
        return ModelDb.AllCards.Where(m => m.AllPortraitPaths.Any(p => p.Contains("HideDetailsMod/")));
    }
    static public IEnumerable<CardModel> GenerateAllVersions(CardModel card) => GenerateAllVersionsInternal(card).Select(e => e.card);
    static public IEnumerable<(CardModel card, CustomImgType type)> GenerateAllVersionsInternal(CardModel card)
    {
        Types.Get(card);
        card.GetOverrideImage();

        IEnumerable<CardImg> Imgs = card.AllPortraitPaths.Where(path => path.Contains("HideDetailsMod/")).Select(CardImg.Of).OfType<CardImg>();

        var both = Imgs.Where(i => !i.IsUpgraded && Imgs.Contains(i.Upgraded()));
        var upgraded = Imgs.Where(i => i.IsUpgraded && !both.Contains(i.Downgraded()));
        var unUpgraded = Imgs.Where(i => !i.IsUpgraded && !both.Contains(i.Downgraded()));

        List<(CardImg i, CustomImgType)> final = [];
        foreach (var i in Imgs)
        {
            if (both.Contains(i)) final.Add((i, CustomImgType.Both));
            if (upgraded.Contains(i)) final.Add((i, CustomImgType.OnlyUpgrade));
            if (unUpgraded.Contains(i)) final.Add((i, CustomImgType.OnlyBase));
        }
        var list = new List<(CardModel card, CustomImgType type)>();

        foreach (var (Img, type) in final)
        {
            var c = card.ToMutable();
            (CardImg? Base, CardImg? Upgraded) Override = type switch
            {
                CustomImgType.OnlyBase => (Img.Downgraded(), Img.Downgraded()),
                CustomImgType.OnlyUpgrade => (Img.Upgraded(), Img.Upgraded()),
                CustomImgType.Both => (Img.Downgraded(), Img.Upgraded()),
                _ => throw new Exception("Unknown CustomImgType: " + type.ToString())
            };
            c.SetOverrideImage(Override);
            Types.Set(c, type);
            if (Img.IsUpgraded) c.UpgradeInternal();
            list.Add((c, type));
        }
        return list;
    }
    public static IEnumerable<string> RemovePlusPairs(IEnumerable<string> list)
    {
        // Filter out item if it ends with "_plus" AND the base item exists in the list
        return list.Where(item => !item.EndsWith("_plus") || !list.Contains(item.Replace("_plus", "")));
    }
}

[ModInterop("STS2Export")]
internal static class Interop
{
    //Being static is optional, but this class in general should never be instantiated.
    [InteropTarget("STS2Export.Exporter.CardExport")]
    internal class CardExport(CardModel model, int upgrades) : InteropClassWrapper;

    [InteropTarget("STS2Export.Exporter.MadScienceExport")]
    internal class MadScienceExport((CardType, TinkerTime.RiderEffect) values, int upgrades) : InteropClassWrapper;
}