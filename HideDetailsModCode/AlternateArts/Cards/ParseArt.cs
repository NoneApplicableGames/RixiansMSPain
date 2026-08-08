using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class ParseArt : AlternateCardArt<Parse>
{
    static CardImg PoorSleep { get; } = new("necrobinder/parse_if_poor_sleep");

    public override CardImg? Get(Parse card)
    {
        return Util.HasCard<PoorSleep>(card.Owner) ? PoorSleep : null;
    }
}
