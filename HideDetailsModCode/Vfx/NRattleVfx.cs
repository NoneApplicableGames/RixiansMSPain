using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode.Vfx;
// NRattleVfx.cs
public partial class NRattleVfx : NCreatureModifierVfx
{
    public float _shakeIntensity = 14f;
    public float _rotationIntensity = 0.08f;

    public static NRattleVfx? Create(NCreatureVisuals visuals, float duration = 0.5f, DurationMode mode = DurationMode.Timed)
    {
        return Create<NRattleVfx>(visuals, duration, mode, vfx =>
        {
            vfx._transType = Tween.TransitionType.Linear;
        });
    }

    protected override void ApplyProgress(float t)
    {
        float currentShake = _shakeIntensity * t;
        CurrentPositionOffset = new Vector2(
            MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-currentShake, currentShake),
            MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-currentShake * 0.5f, currentShake * 0.5f)
        );
        CurrentRotationOffset = MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-_rotationIntensity * t, _rotationIntensity * t);
    }
}