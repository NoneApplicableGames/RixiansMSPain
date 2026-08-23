using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public abstract partial class NCreatureModifierVfx : Node2D
{
    public enum DurationMode
    {
        Timed,
        UntilRevert,
        Permanent
    }

    public Tween? _tween;
    public NCreatureVisuals _creatureVisuals;
    public Vector2 _position;
    public Vector2 _size;
    public DurationMode _mode = DurationMode.Timed;
    public float _duration = 0.4f;

    public Tween.EaseType _easeType = Tween.EaseType.Out;
    public Tween.TransitionType _transType = Tween.TransitionType.Quad;

    protected Node2D? _creatureBody;
    protected Node? _originalBodyParent;
    protected int _originalBodyIndex;
    protected Vector2 _originalBodyLocalPos;
    protected Vector2 _originalBodyScale;
    protected float _originalBodyRotation;

    protected TaskCompletionSource<bool> _manualRevertTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource<bool> _applyCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource<bool> _vfxCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CancellationToken _cancelToken;
    public CancellationTokenSource VfxCancellationToken { get; } = new();

    public Task ApplyTask => _applyCompletedTcs.Task;
    public Task VfxTask => _vfxCompletedTcs.Task;

    /// <summary>
    /// Spawns and automatically inserts the modifier node into the creature's parent hierarchy.
    /// Do NOT call CombatVfxContainer.AddChildSafely on the returned node; it adds itself!
    /// </summary>
    public static TVfx? Create<TVfx>(
        NCreatureVisuals creatureVisuals,
        Vector2 position,
        Vector2 size,
        float duration = 0.4f,
        DurationMode mode = DurationMode.Timed,
        Action<TVfx>? configure = null) where TVfx : NCreatureModifierVfx, new()
    {
        if (TestMode.IsOn || !GodotObject.IsInstanceValid(creatureVisuals))
        {
            return null;
        }

        Node2D body = creatureVisuals.GetCurrentBody();
        if (!GodotObject.IsInstanceValid(body) || body.GetParent() == null)
        {
            return null;
        }

        TVfx vfx = new()
        {
            _creatureVisuals = creatureVisuals,
            _position = position,
            _size = size,
            _duration = duration,
            _mode = mode
        };

        configure?.Invoke(vfx);

        // Add directly as sibling to body, right above it in the tree
        Node parent = body.GetParent();
        int index = body.GetIndex();
        parent.AddChildSafely(vfx);
        parent.MoveChild(vfx, index);

        return vfx;
    }

    public override void _Ready()
    {
        _cancelToken = VfxCancellationToken.Token;
        TaskHelper.RunSafely(PlayVfx());
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        VfxCancellationToken.Cancel();
        _applyCompletedTcs.TrySetCanceled();
        _manualRevertTcs.TrySetCanceled();
        _vfxCompletedTcs.TrySetResult(true);
        KillActiveTween();
    }

    protected Tween ResetTween()
    {
        KillActiveTween();
        _tween = CreateTween();
        return _tween;
    }

    protected void KillActiveTween()
    {
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
            _tween = null;
        }
    }

    private async Task PlayVfx()
    {
        if (_cancelToken.IsCancellationRequested || !GodotObject.IsInstanceValid(_creatureVisuals))
        {
            _applyCompletedTcs.TrySetResult(false);
            _vfxCompletedTcs.TrySetResult(false);
            this.QueueFreeSafely();
            return;
        }

        _creatureBody = _creatureVisuals.GetCurrentBody();
        if (!GodotObject.IsInstanceValid(_creatureBody))
        {
            _applyCompletedTcs.TrySetResult(false);
            _vfxCompletedTcs.TrySetResult(false);
            this.QueueFreeSafely();
            return;
        }

        _originalBodyParent = GetParent();
        _originalBodyIndex = GetIndex();
        _originalBodyLocalPos = _creatureBody.Position;
        _originalBodyScale = _creatureBody.Scale;
        _originalBodyRotation = _creatureBody.Rotation;

        // Align this modifier to the creature's original local transform
        Position = _originalBodyLocalPos;
        Scale = Vector2.One;
        Rotation = 0f;

        // Reparent creature body into this node using Godot's built-in fast reparent
        _creatureBody.Reparent(this, keepGlobalTransform: false);
        _creatureBody.Position = Vector2.Zero;

        try
        {
            if (_cancelToken.IsCancellationRequested) return;

            // 1. Run forward animation (0 -> 1)
            await PlayTweenSequence(from: 0f, to: 1f, _duration * 0.5f);

            // Signal ApplyTask complete!
            _applyCompletedTcs.TrySetResult(true);

            // 2. Lifecycle hold / reverse
            switch (_mode)
            {
                case DurationMode.Timed:
                    if (_cancelToken.IsCancellationRequested) return;
                    await PlayTweenSequence(from: 1f, to: 0f, _duration * 0.5f);
                    break;

                case DurationMode.UntilRevert:
                    using (_cancelToken.Register(() => _manualRevertTcs.TrySetCanceled()))
                    {
                        await _manualRevertTcs.Task;
                    }

                    if (_cancelToken.IsCancellationRequested) return;
                    await PlayTweenSequence(from: 1f, to: 0f, _duration * 0.5f);
                    break;

                case DurationMode.Permanent:
                    break;
            }
        }
        catch (Exception) when (_cancelToken.IsCancellationRequested)
        {
            // Room exit or battle cancellation
        }
        finally
        {
            KillActiveTween();

            // Reparent body and any child modifiers back to original parent
            if (GodotObject.IsInstanceValid(_creatureBody) && GodotObject.IsInstanceValid(_originalBodyParent))
            {
                foreach (Node child in GetChildren())
                {
                    if (child is Node2D child2D)
                    {
                        child2D.Reparent(_originalBodyParent, keepGlobalTransform: false);
                        child2D.Position = _originalBodyLocalPos;
                        _originalBodyParent.MoveChild(child2D, _originalBodyIndex);
                    }
                }
            }

            _applyCompletedTcs.TrySetResult(true);
            _vfxCompletedTcs.TrySetResult(true);

            if (GodotObject.IsInstanceValid(this))
            {
                this.QueueFreeSafely();
            }
        }
    }

    protected abstract void ApplyProgress(float t);

    private async Task PlayTweenSequence(float from, float to, float time)
    {
        Tween tween = ResetTween();
        tween.TweenMethod(Callable.From<float>(ApplyProgress), from, to, time)
            .SetEase(_easeType)
            .SetTrans(_transType);

        await tween.AwaitFinished(this);
    }

    public void Revert()
    {
        _manualRevertTcs.TrySetResult(true);
    }

    public async Task RevertAsync()
    {
        Revert();
        await VfxTask;
    }
}