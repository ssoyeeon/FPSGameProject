using UnityEngine;

public class SlimeEnemy : EnemyBase
{
    [Header("Slime Flavor")]
    [SerializeField] private float extraBounce = 1.5f;

    protected override void OnDamaged(in HitInfo hit)
    {
        base.OnDamaged(in hit);
        
        TryKnockback(-hit.Normal, hit.Force * extraBounce);
        Debug.Log($"{name}: boing!");
    }

    protected override void OnDied(in HitInfo hit)
    {
        Debug.Log($"{name}: *splosh*");
        base.OnDied(in hit);
    }

}
