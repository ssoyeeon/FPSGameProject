using UnityEngine;

[RequireComponent (typeof(Collider))]
public class Door : MonoBehaviour, IHittable
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;

    private bool _isOpen;

    private void Awake()
    {
        if (doorPivot = null) doorPivot = transform;

    }

    public void OnHit(in HitInfo hit)
    {
        _isOpen = !_isOpen;

        float angle = _isOpen ? openAngle : 0f;
        doorPivot.localRotation = Quaternion.Euler(0f, angle, 0f);

        Debug.Log($"{name} door: {(_isOpen ? "OPEN" : "CLOSED")}");

    }

}
