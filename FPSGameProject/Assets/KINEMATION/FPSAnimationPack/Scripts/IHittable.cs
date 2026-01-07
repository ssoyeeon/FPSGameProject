using UnityEngine;

public interface IHittable
{ 
    void OnHit(in HitInfo hit);
}
