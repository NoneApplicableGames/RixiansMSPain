using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts2;

class ClashArt : AlternateCardArt<Clash>
{
    private static CardImg Playable { get; } = new("event/clash_playable");
    public override CardImg? Get(Clash card)
    {
        var isPlayable = Traverse.Create(card).Property<bool>("IsPlayable").Value;
        return isPlayable ? Playable : null;
    }
}
