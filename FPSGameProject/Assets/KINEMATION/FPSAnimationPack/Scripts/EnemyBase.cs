using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour, IHittable, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private float knockbackMultiplier = 1.0f;

    protected int Hp { get; private set; }

    protected virtual void Awake()
    {
        Hp = maxHp;
    }

    public void OnHit(in HitInfo hit)
    {
        TakeDamage(hit.Damage, in hit);
    }

    public void TakeDamage(int amount, in HitInfo hit)
    {
        if (Hp <= 0) return;

        Hp -= Mathf.Max(0, amount);

        OnDamaged(in hit);

        if(Hp <= 0) OnDied(in hit);
    }

    protected virtual void OnDamaged(in HitInfo hit)
    {
        TryKnockback(hit.Direction, hit.Force * knockbackMultiplier);
    }

    protected virtual void OnDied( in HitInfo hit)
    {
        Debug.Log($"{name} died.");
        Destroy(gameObject);
    }

    protected void TryKnockback(Vector3 direction, float force)
    {
        if (TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(direction * force, ForceMode.Impulse);
        else 
            transform.position += direction * (force * 0.02f);
    }

}
