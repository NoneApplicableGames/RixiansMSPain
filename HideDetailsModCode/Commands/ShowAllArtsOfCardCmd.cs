using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;

class ShowAllArtsOfCardCmd : AbstractConsoleCmd
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
}
