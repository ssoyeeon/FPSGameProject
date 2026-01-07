using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHammer : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float range = 3.0f;
    [SerializeField] private float force = 8.0f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Aim")]
    [Tooltip("비우면 main Camera 사용")]
    [SerializeField] private Transform aimOrigin;

    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        if (aimOrigin == null) aimOrigin = transform;
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TryHit();
    }

    private void TryHit()
    {
        if(_cam == null) _cam = Camera.main;
        if(_cam == null) return;

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore ))
        {
            if(hit.collider.TryGetComponent<IHittable>(out var hittable))
            {
                Vector3 dir = (hit.point - ray.origin).normalized;

                var info = new HitInfo(
                    point: hit.point,
                    normal: hit.normal,
                    direction: dir,
                    force: force,
                    damage: damage,
                    attacker: gameObject
                );

                hittable.OnHit(in info);
                return;
            }

            Debug.Log($"Hit {hit.collider.name} but it is not IHittable.");
        }

    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * range);
        Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * range, 0.1f);
    }
#endif
}

// Gizmos helper (에러 방지용)
public static class GizmosExtensions
{
    public static void DrawWireSuggestiveSphere(Vector3 center, float radius)
    {
#if UNITY_EDITOR
        Gizmos.DrawWireSphere(center, radius);
#endif
    }
}
