using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

class VfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "mspainvfx";

    public override string Args => "<int: hand index> <CardVisualEffect> [float: duration] [float: intensity] [int: times]";

    public override string Description => "Apply a visual effect to a card in hand.";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (!RunManager.Instance.IsInProgress)
        {
            return new(success: false, "A run is currently not in progress!");
        }

        if (args.Length < 1 || !int.TryParse(args[0], out int handIndex))
        {
            return new(success: false, $"Arg 1 must be the hand index (int), got '{(args.Length > 0 ? args[0] : "")}'.");
        }
        if (issuingPlayer == null) return new(false, "No player was found");

        var cards = PileType.Hand.GetPile(issuingPlayer).Cards;
        int count = cards.Count;
        if (handIndex < 0 || handIndex >= count)
        {
            return new CmdResult(success: false, $"Invalid hand index {handIndex}. Valid range: 0-{count - 1}.");
        }

        CardModel cardModel = cards[handIndex];

        if (args.Length < 2 || !TryParseEnum<CardVisualEffect>(args[1], out var effect))
        {
            return new CmdResult(success: false, $"Invalid effect. Valid options: {string.Join(", ", Enum.GetNames<CardVisualEffect>())}.");
        }

        float duration = 0.3f;
        float intensity = 15.0f;
        int times = 1;

        if (args.Length >= 3 && !float.TryParse(args[2], out duration))
        {
            return new CmdResult(success: false, $"Invalid duration '{args[2]}'. Must be a float (e.g. 0.3).");
        }

        if (args.Length >= 4 && !float.TryParse(args[3], out intensity))
        {
            return new CmdResult(success: false, $"Invalid intensity '{args[3]}'. Must be a float (e.g. 15.0).");
        }

        if (args.Length >= 5 && (!int.TryParse(args[4], out times) || times < 1))
        {
            return new CmdResult(success: false, $"Invalid loop count '{args[4]}'. Must be an integer >= 1.");
        }

        cardModel.TriggerEffect(effect, duration, intensity, times);

        return new CmdResult(true, $"Playing {effect} (Duration: {duration}s, Intensity: {intensity}, Times: {times}) on card #{handIndex}");
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1 && RunManager.Instance.IsInProgress && CombatManager.Instance.IsInProgress && player != null)
        {
            CardPile pile = PileType.Hand.GetPile(player);
            int count = pile.Cards.Count;
            if (count > 0)
            {
                List<string> candidates = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();
                return CompleteArgument(candidates, [], args.FirstOrDefault() ?? "");
            }
        }

        if (args.Length == 2)
        {
            List<string> candidates = Enum.GetNames<CardVisualEffect>().ToList();
            return CompleteArgument(candidates, [args[0]], args[1]);
        }

        if (args.Length == 3)
        {
            return CompleteArgument(["0.1", "0.3", "0.5", "1.0"], [args[0], args[1]], args[2]);
        }
        if (args.Length == 4)
        {
            return CompleteArgument(["5.0", "15.0", "30.0"], [args[0], args[1]], args[3]);
        }
        if (args.Length == 5)
        {
            return CompleteArgument(["1", "2", "3", "5"], [args[0], args[1]], args[4]);
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
public enum CardVisualEffect : byte
{
    None,
    Rattle,
    Flatten,
    Squeeze,
}

public record CardEffectState
{
    public CardVisualEffect CurrentEffect;
    public float DurationRemaining;
    public float Intensity;
    public int Times = 1;
}

public static class CardModelEffectExtensions
{
    public static void SafelyInsertParent(this Node node, Node newParent)
    {
        if (!node.IsInsideTree())
        {
            ExecuteSafely(node, newParent);
            return;
        }

        // Defer insertion to avoid thread lock and C++ execution engine exceptions
        Callable.From(() => ExecuteSafely(node, newParent)).CallDeferred();
    }

    private static void ExecuteSafely(Node node, Node newParent)
    {
        if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(newParent)) return;
        if (node.GetParent() == newParent) return;

        var parent = node.GetParent();
        if (parent == null)
        {
            newParent.AddChildSafely(node);
            return;
        }

        var index = node.GetIndex();
        if (newParent.GetParent() == null)
        {
            node.AddSiblingSafely(newParent);
        }
        else if (newParent.GetParent() != parent)
        {
            newParent.Reparent(parent);
        }

        parent.MoveChildSafely(newParent, index);
        node.Reparent(newParent);
    }

    public static void TriggerEffect(this CardModel card, CardVisualEffect effect, float duration = 0.3f, float intensity = 15.0f, int times = 1)
    {
        try
        {
            var nCard = NCard.FindOnTable(card);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlayEffect(effect, duration, intensity, times);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[TriggerEffect({card.Id.Entry}, {Enum.GetName(effect)})] " + e.ToString());
        }
    }
}

public partial class NCardCustomVfxContainer : Control
{
    static readonly internal AddedNode<NCard, NCardCustomVfxContainer> Node = new(nCard => new NCardCustomVfxContainer().Set(nCard));

#nullable disable
    public NCard CardNode { get; private set; }
#nullable restore
    public CardModel? CardModel => CardNode?.Model;
    public NCardHolder? Holder => CardNode.GetAncestorOfType<NCardHolder>();

    private Tween? _activeTween;
    private CardModel? _lastBoundModel;
    static readonly NotNullSpireField<CardModel, CardEffectState> EffectState = new(() => new());
    NCardCustomVfxContainer Set(NCard nCard)
    {
        CardNode = nCard;
        CardNode.TreeEntered += OnCardNodeTreeEntered;
        CardNode.TreeExited += OnCardNodeTreeExited;
        return this;
    }

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        EnsureParented();
        Resized += () => PivotOffset = Size / 2;
    }

    private void OnCardNodeTreeEntered()
    {
        // Defer evaluation when tree signals trigger
        Callable.From(EnsureParented).CallDeferred();
    }
    private void OnCardNodeTreeExited()
    {
        ResetAndKillActiveTween();
    }

    private void EnsureParented()
    {
        if (!GodotObject.IsInstanceValid(CardNode.Body) || CardNode.Body.GetParent() == this) return;
        CardNode.Body.SafelyInsertParent(this);
    }

    public override void _Process(double delta)
    {
        if (GodotObject.IsInstanceValid(CardNode) && CardNode.GetParent() != this)
        {
            EnsureParented();
        }

        var currentModel = CardModel;
        if (currentModel != _lastBoundModel)
        {
            _lastBoundModel = currentModel;
            OnModelChanged(currentModel);
        }

        if (currentModel != null)
        {
            var state = EffectState.Get(currentModel);
            if (state.CurrentEffect != CardVisualEffect.None && state.DurationRemaining > 0)
            {
                state.DurationRemaining -= (float)delta;
            }
        }
    }

    private void OnModelChanged(CardModel? model)
    {
        ResetAndKillActiveTween();
        if (model == null) return;

        var state = EffectState.Get(model);
        if (state.CurrentEffect != CardVisualEffect.None && state.DurationRemaining > 0)
        {
            PlayEffect(state.CurrentEffect, state.DurationRemaining, state.Intensity, state.Times);
        }
    }

    public void PlayEffect(CardVisualEffect effect, float duration = 0.3f, float intensity = 15.0f, int times = 1)
    {
        if (CardModel == null || effect == CardVisualEffect.None)
        {
            ResetAndKillActiveTween();
            return;
        }

        switch (effect)
        {
            case CardVisualEffect.Rattle:
                PlayRattle(duration, intensity, times);
                break;
            case CardVisualEffect.Flatten:
                PlayFlatten(duration, intensity, times);
                break;
            case CardVisualEffect.Squeeze:
                PlaySqueeze(duration, intensity, times);
                break;
        }
    }

    public void PlayRattle(float duration = 0.3f, float intensity = 15.0f, int times = 1)
    {
        PlayTween(target =>
        {
            var tween = CreateTween().SetParallel(true).SetLoops(times);
            tween.TweenProperty(target, "position:x", 0.0f, duration)
                .From(intensity)
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(target, "rotation", 0.0f, duration)
                .From(Mathf.DegToRad(intensity * 0.2f))
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            return tween;
        }, CardVisualEffect.Rattle, duration, intensity, times);
    }

    public void PlayFlatten(float duration = 0.2f, float intensity = 0, int times = 1)
    {
        PlayTween(target =>
        {
            var tween = CreateTween().SetLoops(times);
            tween.TweenProperty(target, "scale", new Vector2(1.0f, 0.2f), duration * 0.3f)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(target, "scale", Vector2.One, duration * 0.7f)
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            return tween;
        }, CardVisualEffect.Flatten, duration, intensity: intensity, times: times);
    }

    internal void PlaySqueeze(float duration = 0.2f, float intensity = 0, int times = 1)
    {
        PlayTween(target =>
        {
            var tween = CreateTween().SetLoops(times);
            tween.TweenProperty(target, "scale", new Vector2(0.2f, 1.0f), duration * 0.3f)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(target, "scale", Vector2.One, duration * 0.7f)
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            return tween;
        }, CardVisualEffect.Squeeze, duration, intensity: intensity, times: times);
    }

    private void PlayTween(Func<CanvasItem, Tween> createTweenFunc, CardVisualEffect effect, float duration, float intensity = 0f, int times = 1)
    {
        ResetAndKillActiveTween();

        var state = EffectState.Get(CardModel!);
        state.CurrentEffect = effect;
        state.DurationRemaining = duration * times;
        state.Intensity = intensity;
        state.Times = times;

        _activeTween = createTweenFunc(this);
        _activeTween.Finished += OnEffectTweenFinished;
    }

    private void OnEffectTweenFinished()
    {
        if (CardModel != null)
        {
            var state = EffectState.Get(CardModel);
            state.CurrentEffect = CardVisualEffect.None;
            state.DurationRemaining = 0f;
            state.Intensity = 0f;
            state.Times = 1;
        }
        ResetAndKillActiveTween();
    }

    public void ResetAndKillActiveTween()
    {
        _activeTween?.FastForwardToCompletion();
        _activeTween?.Kill();
        _activeTween = null;

        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = Vector2.One;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IsInstanceValid(CardNode))
            {
                CardNode.TreeEntered -= OnCardNodeTreeEntered;
                CardNode.TreeExited -= OnCardNodeTreeExited;
            }
            ResetAndKillActiveTween();
        }
        base.Dispose(disposing);
    }
}
