using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public partial class NRattleVfx : NCreatureModifierVfx
{
    public float _shakeIntensity = 12f;
    public float _rotationIntensity = 0.08f;

    public static NRattleVfx? Create(
        NCreatureVisuals creatureVisuals,
        Vector2 position,
        Vector2 size,
        float shakeIntensity = 12f,
        float rotationIntensity = 0.08f,
        float duration = 0.5f,
        DurationMode mode = DurationMode.Timed)
    {
        return Create<NRattleVfx>(creatureVisuals, position, size, duration, mode, vfx =>
        {
            vfx._shakeIntensity = shakeIntensity;
            vfx._rotationIntensity = rotationIntensity;
            vfx._transType = Tween.TransitionType.Linear;
            vfx._easeType = Tween.EaseType.InOut;
        });
    }

    protected override void ApplyProgress(float t)
    {
        // Shakes relative to local (0, 0) and smoothly settles at (0, 0) when t == 0
        float currentShake = _shakeIntensity * t;
        float currentRot = _rotationIntensity * t;

        Position = new Vector2(
            Rng.Chaotic.NextFloat(-currentShake, currentShake),
            Rng.Chaotic.NextFloat(-currentShake * 0.5f, currentShake * 0.5f)
        );
        Rotation = Rng.Chaotic.NextFloat(-currentRot, currentRot);
    }
}