using System.Collections.Generic;
using BaseLib.Utils;
using HideDetailsMod.HideDetailsModCode;
using HideDetailsMod.HideDetailsModCode.AlternateArts2;
using MegaCrit.Sts2.Core.Models;

public static class CardOverrideExtensions
{
    internal static SpireField<CardModel, CardImg> Overrides { get; } = new SpireField<CardModel, CardImg>(() => null).CopyOnClone();

    extension(CardModel card)
    {
        // For C# 14 users: true read-write property
        public CardImg? OverrideArtImage
        {
            get => Overrides.Get(card);
            set => Overrides.Set(card, value);
        }

        // For C# 13 users: standard method fallback
        public CardImg? GetOverrideArt() => Overrides.Get(card);

        public void SetOverrideArt(CardImg image) => Overrides.Set(card, image);
        public void ClearOverrideArt() => Overrides.Set(card, null);
    }
}

public class OverrideArt() : IAlternateCardArt(-1)
{
    // Fetches directly from the extension class field
    public override CardImg? Get(CardModel card) => card.OverrideArtImage;

    public override IEnumerable<CardImg> GetAll(CardModel card)
    {
        if (Get(card) is { } img)
        {
            yield return img;
        }
    }
}
