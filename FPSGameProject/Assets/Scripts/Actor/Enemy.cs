// Àû ¿¹Á¦
using UnityEngine;

public class Enemy : Actor
{
    public float speed = 2f;
    public Transform target;

    protected override void ActorUpdate()
    {
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.Translate(dir * speed * Time.deltaTime);
        }
    }
}