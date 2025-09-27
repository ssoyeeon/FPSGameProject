using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f; // 총알 속도
    private Vector3 direction; // 총알이 날아갈 방향

    // 초기화 메서드: 총이 발사될 때 호출하여 방향을 설정합니다.
    public void Initialize(Vector3 shootDirection)
    {
        direction = shootDirection.normalized; // 방향 벡터를 정규화하여 사용
    }

    void Update()
    {
        // 매 프레임마다 지정된 방향으로 이동
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(this.gameObject);
    }
}
