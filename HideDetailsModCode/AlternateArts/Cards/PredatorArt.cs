using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class PredatorArt : AlternateCardArt<Predator>
{
    static CardImg WithGoldAxe { get; } = new("silent/predator_gold_axe");
    public override CardImg? Get(Predator card) =>
        Util.HasCard<GoldAxe>(card.Owner) ? WithGoldAxe : null;
}