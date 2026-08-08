using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class ChargeArt : AlternateCardArt<Charge>
{
    static CardImg ZeroDraw { get; } = new("regent/charge_0_draw");
    static CardImg OneDraw { get; } = new("regent/charge_1_draw");
    public override CardImg? Get(Charge card)
    {
        var drawPile = CardPile.Get(PileType.Draw, card.Owner);
        if (drawPile == null) return null;

        return drawPile.Cards.Count switch
        {
            0 => ZeroDraw,
            1 => OneDraw,
            _ => null
        };
    }
}