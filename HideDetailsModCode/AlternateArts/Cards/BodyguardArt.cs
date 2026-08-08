using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class BodyguardArt : AlternateCardArt<Bodyguard>
{
    static CardImg Protector { get; } = new("necrobinder/bodyguard_if_protector");

    public override CardImg? Get(Bodyguard card)
    {
        return Util.HasCard<Protector>(card.Owner) ? Protector : null;
    }
}
