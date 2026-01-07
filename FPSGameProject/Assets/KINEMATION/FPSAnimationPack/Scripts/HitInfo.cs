using UnityEngine;

public readonly struct HitInfo 
{
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly Vector3 Direction;
    public readonly float Force;
    public readonly int Damage;
    public readonly GameObject Attacker;

    public HitInfo(Vector3 point, Vector3 normal, Vector3 direction, float force, int damage, GameObject attacker)
    {
        Point = point; 
        Normal = normal; 
        Direction = direction; 
        Force = force; 
        Damage = damage; 
        Attacker = attacker; 
    }
}
