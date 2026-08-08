using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class TheGambitArt : AlternateCardArt<TheGambit>
{
    static CardImg NoBlock { get; } = new("colorless/the_gambit_no_block");

    public override CardImg? Get(TheGambit card)
    {
        if (card.DynamicVars.Block.IntValue <= 0) return NoBlock;
        if (card.Owner.HasPower<NoBlockPower>()) return NoBlock;
        return null;
    }
}
