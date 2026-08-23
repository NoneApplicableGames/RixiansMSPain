using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode.Vfx;
// NSqueezeVfx.cs
public partial class NSqueezeVfx : NCreatureModifierVfx
{
    public float _horizontalCompression = 0.65f;
    public float _verticalStretch = 1.35f;

    public static NSqueezeVfx? Create(NCreatureVisuals visuals, float duration = 0.4f, DurationMode mode = DurationMode.Timed)
    {
        return Create<NSqueezeVfx>(visuals, duration, mode, vfx =>
        {
            vfx._transType = Tween.TransitionType.Back;
        });
    }

    protected override void ApplyProgress(float t)
    {
        CurrentScaleMultiplier = new Vector2(
            Mathf.Lerp(1.0f, _horizontalCompression, t),
            Mathf.Lerp(1.0f, _verticalStretch, t)
        );
    }
}