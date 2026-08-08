using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class SharedFateArt : AlternateCardArt<SharedFate>
{
    static CardImg Friendship { get; } = new("necrobinder/shared_fate_if_friendship");

    public override CardImg? Get(SharedFate card)
    {
        if (Util.HasCard<Friendship>(card.Owner) || card.Owner.HasPower<FriendshipPower>())
        {
            return Friendship;
        }
        return null;
    }
}
