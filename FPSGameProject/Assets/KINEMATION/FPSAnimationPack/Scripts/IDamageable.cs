using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount, in HitInfo hit);

}
