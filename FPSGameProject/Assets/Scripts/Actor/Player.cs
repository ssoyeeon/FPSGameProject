using UnityEngine;

public class Player : Actor
{
    public float speed = 5f;

    protected override void ActorUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v);
        transform.Translate(dir * speed * Time.deltaTime);
    }
}