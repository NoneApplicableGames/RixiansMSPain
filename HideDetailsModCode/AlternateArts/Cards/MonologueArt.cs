using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class MonologueArt : AlternateCardArt<Monologue>
{
    static CardImg IfLunarBlast { get; } = new("regent/monologue_if_lunar_blast");

    public override CardImg? Get(Monologue card)
    {
        return Util.HasCard<LunarBlast>(card.Owner) ? IfLunarBlast : null;
    }
}