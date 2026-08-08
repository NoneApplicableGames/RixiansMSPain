using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode;

static class StrExtension
{
    public static string GetTextAfter(this string source, string marker)
    {
        // Check for null or empty inputs to prevent runtime crashes
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(marker))
        {
            return string.Empty;
        }

        // Find the starting index of the marker
        int index = source.IndexOf(marker);

        // If marker is not found, return empty (or return source based on your needs)
        if (index == -1)
        {
            return string.Empty;
        }

        // Cut the string starting exactly after the marker
        return source.Substring(index + marker.Length);
    }
}
public record CardImg(string Path)
{
    static public CardImg? Of(string fullPath)
    {
        if (!fullPath.Contains("HideDetailsMod")) return null;
        var text = "HideDetailsMod/images/atlases/card_atlas.sprites/";
        if (fullPath.Contains(text))
        {
            var result = fullPath.GetTextAfter(text);
            return new(result.GetBaseName());
        }
        text = "res://HideDetailsMod/artist_assets/";
        if (fullPath.Contains(text))
        {
            var result = fullPath.GetTextAfter(text);
            return new(result.GetBaseName());
        }
        return null;
    }

    public CardImg(CardModel card) : this($"{card.Pool.Title.ToLowerInvariant()}/{card.Id.Entry.ToLowerInvariant()}") { }
    public static CardImg Upgraded(CardModel card) => new CardImg(card).Upgraded();
    public string PortraitPath => $"res://HideDetailsMod/images/atlases/card_atlas.sprites/{Path}.tres";
    public string PortraitPngPath => $"res://HideDetailsMod/artist_assets/{Path}.png";
    // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
    internal bool Exists() => ResourceLoader.Exists(PortraitPath);
    public bool IsUpgraded => Path.EndsWith("_plus");
    public CardImg Upgraded() => IsUpgraded ? this : new(Path + "_plus");
    public CardImg Downgraded() => IsUpgraded ? new(Path[..Path.LastIndexOf("_plus")]) : this;
    public bool IsBeta => Path.Contains("/beta/");
    public CardImg Beta() => IsBeta ? this : new(Path.Replace("/", "/beta/"));

    public CardImg NonBeta() => !IsBeta ? this : new(Path.Replace("/beta/", "/"));
}
abstract public class ICardImgFactory(IEnumerable<string> AllPaths)
{
    internal IEnumerable<string> AllPaths { get; } = AllPaths;
    public IEnumerable<CardImg> AllPathsAsImg => AllPaths.Select(Path => new CardImg(Path));
    internal IEnumerable<CardImg> AllNormal => AllPathsAsImg.Where(Img => Img.Exists());
    internal IEnumerable<CardImg> AllUpgraded => AllPathsAsImg.Select(Path => Path.Upgraded()).Where(Img => Img.Exists());
    public IEnumerable<CardImg> All => [.. AllNormal, .. AllUpgraded];

    abstract public bool IsFor(CardModel card);
    abstract public CardImg? Get(CardModel card);

    public abstract void OnCardGenerated(AbstractModel thisModel, CardModel generatedCard);
    public abstract void OnCardPlayed(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardPlay cardPlay);
    public abstract void OnCardExhausted(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal);
    public abstract void OnPowerApplied(AbstractModel thisModel, PlayerChoiceContext choiceContext, PowerModel power, decimal amount);
    public abstract void OnDeath(AbstractModel thisModel, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented);
    public abstract void OnTurnEnd(AbstractModel thisModel, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants);
    public abstract void OnTurnStart(AbstractModel thisModel, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState);
    public abstract void OnCardDrawn(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw);
    public abstract void OnCardEnchanted(CardModel thisModel, EnchantmentModel enchantment, decimal amount);
}

public class CardImgFactory2<T>(IEnumerable<string> AllPaths, Func<T, string?> Condition) : ICardImgFactory(AllPaths) where T : CardModel
{
    public CardImgFactory2(string Path, Func<T, bool?> Condition)
        : this([Path], card => Condition(card) ?? false ? Path : null) { }
    public CardImgFactory2() : this([], card => null) { }
    public override bool IsFor(CardModel card) => card is T;
    public override CardImg? Get(CardModel card)
    {
        if (card is not T _card)
        {
            MainFile.Logger.Error($"Attempted to Get an alt art img for {card.Id} without checking IsFor first. Expected {typeof(T)}");
            return null;
        }
        try
        {
            var result = Condition(_card);
            if (result == null) return null;
            return new(result);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Error running condition for ${card.Id}: {e}");
        }
        return null;
    }
    public Action<T, CardModel>? WhenCardGenerated { get; set; }
    public Action<T, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; }
    public Action<T, PlayerChoiceContext, CardModel>? WhenCardExhausted { get; set; }
    public Action<T, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; }
    public Action<T, PlayerChoiceContext, Creature, bool>? AfterDeath { get; set; }
    public Action<T, PlayerChoiceContext, CombatSide, IEnumerable<Creature>>? WhenTurnEnd { get; set; }
    public Action<T, CombatSide, IEnumerable<Creature>, ICombatState>? WhenTurnStart { get; set; }
    public Action<T, PlayerChoiceContext, CardModel, bool>? WhenCardDrawn { get; set; }
    public Action<T, EnchantmentModel, decimal>? WhenCardEnchanted { get; set; }

    public override void OnCardGenerated(AbstractModel thisModel, CardModel generatedCard)
    { if (thisModel is T self) WhenCardGenerated?.Invoke(self, generatedCard); }
    public override void OnCardPlayed(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    { if (thisModel is T self) WhenCardPlayed?.Invoke(self, choiceContext, cardPlay); }
    public override void OnCardExhausted(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    { if (thisModel is T self) WhenCardExhausted?.Invoke(self, choiceContext, card); }
    public override void OnPowerApplied(AbstractModel thisModel, PlayerChoiceContext choiceContext, PowerModel power, decimal amount)
    { if (thisModel is T self) WhenPowerApplied?.Invoke(self, choiceContext, power, amount); }
    public override void OnDeath(AbstractModel thisModel, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented)
    { if (thisModel is T self) AfterDeath?.Invoke(self, choiceContext, creature, wasRemovalPrevented); }
    public override void OnTurnEnd(AbstractModel thisModel, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    { if (thisModel is T self) WhenTurnEnd?.Invoke(self, choiceContext, side, participants); }
    public override void OnTurnStart(AbstractModel thisModel, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    { if (thisModel is T self) WhenTurnStart?.Invoke(self, side, participants, combatState); }
    public override void OnCardDrawn(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    { if (thisModel is T self) WhenCardDrawn?.Invoke(self, choiceContext, card, fromHandDraw); }
    public override void OnCardEnchanted(CardModel thisModel, EnchantmentModel enchantment, decimal amount)
    { if (thisModel is T self) WhenCardEnchanted?.Invoke(self, enchantment, amount); }
}
