using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class OutbreakArt : AlternateCardArt<Outbreak>
{
    static CardImg IfNoxious { get; } = new("silent/outbreak_if_noxious_fumes");

    public override CardImg? Get(Outbreak card)
    {
        // MainFile.Logger.Debug($"[Alt Art] [Outbreak] Checking for NoxiousFumes");
        if (Util.HasCard<NoxiousFumes>(card.Owner) || card.Owner.HasPower<NoxiousFumesPower>())
        {
            return IfNoxious;
        }

        return null;
    }
}

// new CardImgFactory2<SpoilsOfBattle>("regent/spoils_of_battle_if_falling_star_played", card => {
//         if (card.IsCanonical) return null;
//         var me = Util.GetOwner(card);
//         if (me == null) return null;
//         var PlayedFallingStarThisCombat = CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.Actor == me.Creature && entry.CardPlay.Card is FallingStar);
//         return PlayedFallingStarThisCombat;
//     })