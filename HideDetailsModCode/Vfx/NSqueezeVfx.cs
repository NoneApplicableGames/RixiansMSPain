using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

/// <summary>
/// Bottle-squeeze VFX: narrows width (X) and stretches height (Y).
/// </summary>
public partial class NSqueezeVfx : NCreatureModifierVfx
{
    // Width factor when fully squeezed (e.g., 0.65 = 35% narrower)
    public float _horizontalCompression = 0.65f;

    // Height factor when fully squeezed (e.g., 1.35 = 35% taller)
    public float _verticalStretch = 1.35f;

    public static NSqueezeVfx? Create(
        NCreatureVisuals creatureVisuals,
        Vector2 position,
        Vector2 size,
        float horizontalCompression = 0.65f,
        float verticalStretch = 1.35f,
        float duration = 0.4f,
        DurationMode mode = DurationMode.Timed)
    {
        return Create<NSqueezeVfx>(creatureVisuals, position, size, duration, mode, vfx =>
        {
            vfx._horizontalCompression = horizontalCompression;
            vfx._verticalStretch = verticalStretch;
            vfx._transType = Tween.TransitionType.Back;
            vfx._easeType = Tween.EaseType.Out;
        });
    }

    // NSqueezeVfx.cs
    protected override void ApplyProgress(float t)
    {
        Scale = new Vector2(
            Mathf.Lerp(1.0f, _horizontalCompression, t),
            Mathf.Lerp(1.0f, _verticalStretch, t)
        );
    }
}