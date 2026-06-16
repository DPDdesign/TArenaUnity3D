using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class TosterSfxSet : MonoBehaviour
{
    public AudioClip[] attackSfx;
    public AudioClip[] hitSfx;
    public AudioClip[] deathSfx;

    public void PlayAttack()
    {
        CombatSfxManager.PlayRandomSfx(attackSfx);
    }

    public void PlayHit()
    {
        CombatSfxManager.PlayRandomSfx(hitSfx);
    }

    public void PlayDeath()
    {
        CombatSfxManager.PlayRandomSfx(deathSfx);
    }
}
