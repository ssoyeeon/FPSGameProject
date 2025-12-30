using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요
using KINEMATION.FPSAnimationPack.Scripts.Player; // FPSPlayer를 찾기 위해 필요

public class FPSDisplay : MonoBehaviour
{
    public FPSPlayer player;      // FPSPlayer 게임오브젝트를 연결
    public TextMeshProUGUI ammoText; // UI 텍스트(TMP)를 연결

    void Update()
    {
        if (player == null || ammoText == null) return;

        // 1. 플레이어가 현재 들고 있는 무기를 가져옵니다.
        var currentWeapon = player.GetActiveWeapon();

        if (currentWeapon != null)
        {
            // 2. 무기에서 현재 탄약과 최대 탄약을 가져옵니다.
            int currentAmmo = currentWeapon.GetActiveAmmo();
            int maxAmmo = currentWeapon.GetMaxAmmo();

            // 3. 텍스트로 띄웁니다. (예: 30 / 30)
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
        }
    }
}
