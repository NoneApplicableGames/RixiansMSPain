using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class LanternKeyArt : AlternateCardArt<LanternKey>
{
    static CardImg Bread { get; } = new("quest/lantern_key_if_bread");

    public override CardImg? Get(LanternKey card)
    {
        return card.Owner.Relics.OfType<Bread>().Any() ? Bread : null;
    }
}