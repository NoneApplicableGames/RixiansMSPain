using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class CalculatedGambleArt : AlternateCardArt<CalculatedGamble>
{
    static CardImg NoDraw { get; } = new("silent/calculated_gamble_no_draw");
    public override CardImg? Get(CalculatedGamble card)
    {
        var HasFiddle = card.Owner.Relics.Any(relic => relic is Fiddle);
        var HasNoDrawPower = card.Owner.HasPower<NoDrawPower>();

        if (HasFiddle || HasNoDrawPower) return NoDraw;
        return null;
    }
}