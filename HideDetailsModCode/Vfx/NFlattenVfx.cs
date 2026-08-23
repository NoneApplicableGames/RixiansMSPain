using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public partial class NFlattenVfx : NCreatureModifierVfx
{
    public Vector2 _squashScale = new(1.5f, 0.4f);

    public static NFlattenVfx? Create(NCreatureVisuals visuals, Vector2? squashScale = null, float duration = 0.4f, DurationMode mode = DurationMode.Timed)
    {
        return Create<NFlattenVfx>(visuals, duration, mode, vfx =>
        {
            vfx._squashScale = squashScale ?? new Vector2(1.5f, 0.4f);
            vfx._transType = Tween.TransitionType.Back;
        });
    }

    protected override void ApplyProgress(float t)
    {
        CurrentScaleMultiplier = Vector2.One.Lerp(_squashScale, t);
    }
}