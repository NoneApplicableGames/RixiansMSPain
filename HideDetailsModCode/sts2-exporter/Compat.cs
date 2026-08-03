using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode;
using HideDetailsMod.HideDetailsModCode.Patches;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;
using static MegaCrit.Sts2.Core.Models.CardModel;

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
[HarmonyPatch("STS2Export.Exporter.CardExport", "ID", MethodType.Getter)]
static class ExporterIdsPatch
{
    static internal void Postfix(ref string __result, CardModel ___model)
    {
        var Img = CardImg.Of(___model.PortraitPath);
        if (Img == null)
        {
            return;
        }
        var Canonical = new CardImg(___model);
        if (Img == Canonical)
        {
            return;
        }
        if (Img == Canonical.Upgraded())
        {
            __result += "_plus";
            return;
        }
        var extra = Img.Path.GetTextAfter(Canonical.Path);
        if (extra.StartsWith('_')) extra = extra[1..];
        if (string.IsNullOrEmpty(extra))
        {
            return;
        }
        __result = __result + "-" + extra;
    }
}

[HarmonyPatch]
static class Compat
{
    static readonly MethodInfo targetMethod = AccessTools.Method("STS2Export.Exporter.CardExport:FindAll");
    static internal bool Prepare() => targetMethod != null;
    static internal MethodBase TargetMethod() => targetMethod;
    static internal void Postfix(ref IList __result)
    {
        // if (!MyModConfig.ShouldPatchCardExporter) return true;
        var All = ModelDb.AllCards
                    .Where(m => m.ShouldShowInCardLibrary)
                    .SelectMany(GenerateAllVersionsInternal)
                    .SelectMany<(CardModel card, CustomImgType type), (CardModel, int)>(m =>
                    {
                        CardModel Clone(bool upgrade = false)
                        {
                            var clone = (CardModel)m.card.Downgrade().MutableClone();
                            if (upgrade) clone.Downgrade();
                            return clone;
                        }
                        return m.type switch
                        {
                            CustomImgType.OnlyBase => [(Clone(), 0)],
                            CustomImgType.OnlyUpgrade => [(Clone(), 1)],
                            CustomImgType.Both => [(Clone(), 0)/*, (Clone(), 1)*/],
                            _ => throw new NotImplementedException()
                        };
                    }
                    ).ToList();

        var existing = __result;

        Type cardType = targetMethod!.DeclaringType!;
        // 2. Create the generic list
        Type listType = typeof(List<>).MakeGenericType(cardType)!;
        IList cardList = (IList)Activator.CreateInstance(listType)!;

        var instantiated = All.Select(m => Activator.CreateInstance(cardType, m.Item1, m.Item2));

        foreach (var item in existing)
        {
            if (item.GetType().Name == "MadScienceExport") cardList.Add(item);
        }
        foreach (var item in instantiated)
        {
            cardList.Add(item);
        }

        __result = cardList;

        // return false;
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
            var c = (CardModel)card.MutableClone();
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
    [HarmonyPatch]
    public static class AnotherOne
    {
        [HarmonyPostfix]
        [HarmonyPatch("STS2Export.Exporter.ItemList", "FindAll")]
        static internal void StopWastingMyTime(object __instance)
        {
            var traverse = new Traverse(__instance);
            string[] toClear = ["Relics", "Potions", "Events", "Creatures", "Enchantments", "Keywords", "Afflictions"];
            foreach (var item in toClear)
            {
                traverse.Field(item).GetValue<IList>().Clear();
            }
            // var cards = traverse.Field("Cards").GetValue<IList>();
        }

        [HarmonyPrefix]
        [HarmonyPatch("STS2Export.Exporter.CardExport", "ProcessCombinedDescription")]
        static internal bool DontBotherWithDesc(ref string __result)
        {
            __result = "";
            return false;
        }
        [HarmonyPrefix]

        [HarmonyPatch("STS2Export.Exporter.CardExport", "Description", MethodType.Getter)]
        static internal bool DontBotherWithDesc2(ref string __result)
        {
            __result = "";
            return false;
        }

        [HarmonyPrefix]

        [HarmonyPatch("STS2Export.Exporter.ExportBatch", "ExportAllData")]
        static internal bool ExportAllData()
        {
            return false;
        }

        static Type CardExportType => targetMethod!.DeclaringType!;
        static object Create(CardModel model, int upgrades) => Activator.CreateInstance(CardExportType, model, upgrades);

        [HarmonyPrefix]

        [HarmonyPatch("STS2Export.Exporter.CardExport", "UpgradedVersion", MethodType.Getter)]
        static internal bool UpgradedVersion(ref object? __result, CardModel ___model)
        {
            if (Compat.Types.Get(___model) is { } type)
            {
                if (type == CustomImgType.Both && !___model.IsUpgraded)
                {
                    __result = Create(___model.Downgrade(), 1);
                    return false;
                }
            }
            __result = null;
            //   public CardExport UpgradedVersion
            //   => this.Upgrades < this.model.MaxUpgradeLevel ? new CardExport(this.model.CanonicalInstance, this.Upgrades + 1) : (CardExport) null;

            //   }
            return false;
        }
    }
    [HarmonyPatch(typeof(CardModel), "GetDescriptionForPile", [typeof(PileType), typeof(DescriptionPreviewType), typeof(Creature)])]
    public static class GetDescriptionForPilePatch
    {
        static bool Prefix(ref string __result)
        {
            __result = "";
            return false;
        }
    }


    [HarmonyPatch]
    public static class CardExportUpgradedVersionPatch
    {
        public static CardModel ReplaceCanonical(CardModel card)
        {
            // Your custom logic goes here
            var clone = (CardModel)card.MutableClone();
            clone.DowngradeInternal();
            return clone;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            Type cardExportType = AccessTools.TypeByName("STS2Export.Exporter.CardExport");
            if (cardExportType == null) throw new Exception("CardExport type not found.");

            // First, check for a property getter
            MethodInfo getter = AccessTools.PropertyGetter(cardExportType, "UpgradedVersion");
            if (getter != null) return getter;

            // Fallback: If it's a standard method or compiler-generated block, find it by name
            MethodInfo method = AccessTools.Method(cardExportType, "get_UpgradedVersion");
            if (method != null) return method;

            throw new Exception("Could not find the UpgradedVersion method or property getter.");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            // Case-insensitive check targeting any method operand that contains "CanonicalInstance"
            matcher.MatchStartForward(new CodeMatch(i =>
                (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) &&
                i.operand is MethodBase m &&
                m.Name.IndexOf("CanonicalInstance", StringComparison.OrdinalIgnoreCase) >= 0
            ));

            if (matcher.IsInvalid)
            {
                // Fail out gracefully while logging the raw IL instructions to debug exactly what is wrong
                Console.WriteLine("--- HARMONY TRANSPILER FAILURE: RAW IL CODES START ---");
                foreach (var ins in instructions)
                {
                    Console.WriteLine($"{ins.opcode} -> {ins.operand} (Type: {ins.operand?.GetType().Name ?? "null"})");
                }
                Console.WriteLine("--- HARMONY TRANSPILER FAILURE: RAW IL CODES END ---");

                throw new InvalidOperationException("Could not locate the CanonicalInstance method invocation inside the bytecode stream.");
            }

            // Swap out the call completely with your static method hook
            MethodInfo replacementMethod = AccessTools.Method(typeof(CardExportUpgradedVersionPatch), nameof(ReplaceCanonical));

            return matcher
                .Set(OpCodes.Call, replacementMethod)
                .InstructionEnumeration();
        }
    }

}

[HarmonyPatch]
public static class CardExportConstructorTranspiler
{
    public static bool Prepare() => Compat.Prepare();
    // 1. Target the constructor cleanly using 'null' to wildcard the unknown CardModel type
    public static MethodBase TargetMethod()
    {
        var type = Compat.TargetMethod().DeclaringType;

        if (type == null) throw new Exception("CardExport type not found.");

        // Find the public instance constructor with exactly 2 parameters
        var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(c => c.GetParameters().Length == 2);

        if (ctor == null) throw new Exception("Target constructor with 2 arguments not found.");
        return ctor;
    }

    // 2. Transpile using a valid inline predicate match
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            // Find ToMutable() callvirt
            .MatchStartForward(new CodeMatch(OpCodes.Callvirt, AccessTools.Method("MegaCrit.Sts2.Core.Models.CardModel:ToMutable")))
            .ThrowIfInvalid("Could not locate the ToMutable() callvirt instruction.")
            // Replace callvirt ToMutable with call MyCustomToMutable
            .SetInstruction(
                 CodeInstruction.Call(typeof(CardExportConstructorTranspiler), nameof(MyCustomToMutable))
            )
            .InstructionEnumeration();
    }
    // Your custom code that runs INSTEAD of ToMutable()
    public static CardModel MyCustomToMutable(CardModel originalModel)
    {
        if (originalModel.IsCanonical) originalModel = originalModel.ToMutable();
        return originalModel;
    }
}
