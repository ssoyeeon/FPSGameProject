using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float damage;
    public float speed;

    [SerializeField] private float moveSpeed = 5f;    // 움직일 속도
    public float mouseSensitivity = 2f;               // 마우스 감도
    public Rigidbody rb;                              // 플레이어 리지드바디
    public Transform cameraTransform;                 // 카메라 (시선 처리용)
    public LayerMask groundMask;                      // 플레이어가 밟을 땅 

    private float verticalRotation = 0f;
    private Vector3 moveDirection;

    bool isGrounded;
    float jumpTime;
    public float jumpForce;

    public GameObject bulletPrefab;
    private SimpleRecoil recoil;

    public GameObject gunGameObject; // 총알이 발사될 위치 (총구 위치를 나타내는 오브젝트)

    // 🔥 추가된 변수: 발사 제어 관련
    public float fireRate = 0.2f;           // 발사 간격 (0.2초)
    private float nextFireTime = 0f;        // 다음 발사가 가능한 시간

    void Start()
    {
        recoil = FindObjectOfType<SimpleRecoil>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Shoot()
    {
        // 1. 카메라 반동 즉시 적용
        if (recoil != null)
        {
            recoil.ApplyRecoil();
        }

        StartCoroutine(SpawnBulletAfterDelay(0.01f));
    }

    private IEnumerator SpawnBulletAfterDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        GameObject bulletObject = Instantiate(bulletPrefab, gunGameObject.transform.position, Quaternion.identity);

        Vector3 shootDirection = cameraTransform.forward;

        Bullet bulletScript = bulletObject.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(shootDirection);
        }
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded == true && jumpTime <= 0)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpTime = 0.8f;
            isGrounded = false;
        }

        if (isGrounded == false)
        {
            jumpTime -= Time.deltaTime;
            if (jumpTime < 0)
            {
                jumpTime = 0;
                isGrounded = true;
            }
        }
        else moveSpeed = 5;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        moveDirection = transform.right * moveX + transform.forward * moveZ;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

    }

    private void FixedUpdate()
    {
        Movement();
    }
    void Movement()
    {
        Vector3 targetVelocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);

        Vector3 velocityChange = (targetVelocity - rb.velocity);
        velocityChange.y = 0;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
}