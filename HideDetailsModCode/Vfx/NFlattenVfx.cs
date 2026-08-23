using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public partial class NFlattenVfx : NCreatureModifierVfx
{
    public Vector2 _squashScale = new(1.5f, 0.4f);

    public static NFlattenVfx? Create(
        NCreatureVisuals creatureVisuals,
        Vector2 position,
        Vector2 size,
        Vector2? squashScale = null,
        float duration = 0.4f,
        DurationMode mode = DurationMode.Timed)
    {
        return Create<NFlattenVfx>(creatureVisuals, position, size, duration, mode, vfx =>
        {
            vfx._squashScale = squashScale ?? new Vector2(1.5f, 0.4f);
            vfx._transType = Tween.TransitionType.Back;
            vfx._easeType = Tween.EaseType.Out;
        });
    }

    // NSquashVfx.cs
    protected override void ApplyProgress(float t)
    {
        // Pure relative multiplier (1.0 -> target squash)
        Scale = Vector2.One.Lerp(_squashScale, t);
    }
}