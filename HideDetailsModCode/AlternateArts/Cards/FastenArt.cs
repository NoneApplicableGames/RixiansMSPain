using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class FastenArt : AlternateCardArt<Fasten>
{
    private static CardImg IfBound { get; } = new("colorless/fasten_if_bound");

    public override CardImg? Get(Fasten card)
    {
        if (card.Affliction is Bound) return IfBound;
        return null;
    }
}