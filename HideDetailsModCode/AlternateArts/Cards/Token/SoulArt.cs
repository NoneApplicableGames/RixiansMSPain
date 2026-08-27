using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class SoulArt : AlternateCardArt<Soul>
{
    static CardImg Freddy { get; } = new("token/beta/soul");

    public override IEnumerable<CardImg> GetAll(Soul card)
    {
        if (MainFile.IsCanary) yield return Freddy;
    }

    protected override bool ShowIfCanonical => true;

    public override CardImg? Get(Soul card)
    {
        NetModSettings netModSettings = card.IsCanonical ? new() : ConfigFrom(card);
        return netModSettings.BetaSoul ? Freddy : null;
    }
}