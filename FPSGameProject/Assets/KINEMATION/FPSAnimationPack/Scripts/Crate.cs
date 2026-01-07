using Unity.VisualScripting;
using UnityEngine;

[RequireComponent (typeof(Collider))]
public class Crate : MonoBehaviour
{
    [SerializeField] private int hp = 2;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinCount = 3;

    public void OnHit(in HitInfo hit)
    {
        hp -= Mathf.Max(1, hit.Damage);
        Debug.Log($"{name} crate hit! hp={hp}");

        if(TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(hit.Direction * hit.Force, ForceMode.Impulse);

        if (hp <= 0)
            Break(hit.Point);

    }

    private void Break(Vector3 point)
    {
        Debug.Log($"{name} broke!");

        if(coinPrefab != null)
        {
            for(int i = 0; i < coinCount; i++)
            {
                Vector3 spawnPos = point + Random.insideUnitSphere * 0.3f;
                Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            }
        }

        Destroy(gameObject);

    }
}
