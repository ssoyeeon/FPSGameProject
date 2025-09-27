using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    [Tooltip("총을 쏠 때 카메라가 위로 올라가는 각도 (음수 값 사용)")]
    public float kickUpAngle = -0.5f; // X축 위로 반동 (위로 튀려면 음수)

    [Tooltip("반동에서 원래 위치로 돌아오는 속도")]
    public float returnSpeed = 5.0f;

    private Vector3 targetRecoilRotation; // 최종 목표 회전값

    void Update()
    {
        // 1. 목표 회전(targetRecoilRotation)을 원래 위치(Vector3.zero)로 부드럽게 복구
        targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, Time.deltaTime * returnSpeed);

        // 2. 현재 카메라의 로컬 회전을 목표 회전으로 설정
        // Time.deltaTime을 곱하여 프레임 속도에 독립적인 부드러운 움직임을 만듭니다.
        transform.localRotation = Quaternion.Euler(targetRecoilRotation);
    }

    /// <summary>
    /// 총을 쏠 때 호출하여 반동을 발생시킵니다.
    /// </summary>
    public void ApplyRecoil()
    {
        // 목표 회전의 X축에만 설정된 반동 값을 즉시 더합니다.
        // Y와 Z는 0을 유지하여 위쪽으로만 움직이게 합니다.
        targetRecoilRotation.x += kickUpAngle;
    }
}
