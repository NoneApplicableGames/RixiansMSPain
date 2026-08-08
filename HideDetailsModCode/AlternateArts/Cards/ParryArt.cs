using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class ParryArt : AlternateCardArt<Parry>
{
    static ParryArt()
    {
        RunManager.Instance.RunStarted += ResetSeen;
    }
    static void ResetSeen(object? _)
    {
        WasSeen = false;
    }
    static CardImg Alt { get; } = new("regent/parry_alt");
    static bool WasSeen = false;
    public override CardImg? Get(Parry card)
    {
        if (IsBeingInspected)
        {
            WasSeen = true;
            return null;
        }
        return (IsInCardLibrary || IsInShop) && !WasSeen ? Alt : null;
    }
}