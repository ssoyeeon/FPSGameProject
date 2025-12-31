// Designed by KINEMATION, 2025.

using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEngine;

namespace KINEMATION.FPSAnimationPack.Scripts.Weapon
{
    [AddComponentMenu("KINEMATION/FPS Animation Pack/Weapon/FPS Weapon")]
    public class FPSWeapon : MonoBehaviour, IAmmoProvider
    {
        public float UnEquipDelay => unEquipDelay;
        public FireMode ActiveFireMode => fireMode;

        public FPSWeaponSettings weaponSettings;
        public Transform aimPoint;

        [SerializeField] protected FireMode fireMode = FireMode.Semi;

        [HideInInspector] public KTransform rightHandPose;
        [HideInInspector] public KTransform adsPose;

        protected GameObject ownerPlayer;
        protected RecoilAnimation recoilAnimation;
        protected FPSWeaponSound weaponSound;

        protected Animator characterAnimator;
        protected Animator weaponAnimator;

        // 애니메이터 파라미터 해싱 (성능 최적화용)
        protected static int RELOAD_EMPTY = Animator.StringToHash("Reload_Empty");
        protected static int RELOAD_TAC = Animator.StringToHash("Reload_Tac");
        protected static int FIRE = Animator.StringToHash("Fire");
        protected static int FIREOUT = Animator.StringToHash("FireOut");

        protected static int EQUIP = Animator.StringToHash("Equip");
        protected static int EQUIP_OVERRIDE = Animator.StringToHash("Equip_Override");
        protected static int UNEQUIP = Animator.StringToHash("UnEquip");
        protected static int IDLE = Animator.StringToHash("Idle");

        protected float unEquipDelay;
        protected float emptyReloadDelay;
        protected float tacReloadDelay;

        protected int _activeAmmo;

        protected bool _isReloading;
        protected bool _isFiring;

        protected FPSCameraAnimator cameraAnimator;
        // 기존 ownerPlayer 변수 밑에 추가
        protected FPSPlayer playerScript; // [추가] FPSPlayer 스크립트 저장용

        // [초기화 함수]
        // 게임 시작 시 무기의 주인(플레이어), 애니메이터, 사운드, 반동 컴포넌트 등을 연결하고
        // 무기의 발사 속도나 애니메이션 길이 같은 기본 설정을 준비합니다.
        public virtual void Initialize(GameObject owner)
        {
            ownerPlayer = owner;
            playerScript = owner.GetComponent<FPSPlayer>(); // [추가] 플레이어 스크립트 찾아오기
            recoilAnimation = owner.GetComponent<RecoilAnimation>();
            characterAnimator = owner.GetComponent<Animator>();

            _activeAmmo = weaponSettings.ammo;

            weaponAnimator = GetComponentInChildren<Animator>();
            if (weaponAnimator == null)
            {
                Debug.LogWarning("FPSWeapon: Animator not found!");
            }

            weaponSound = GetComponentInChildren<FPSWeaponSound>();
            if (weaponSound == null)
            {
                Debug.LogWarning("FPSWeapon: FPS Weapon Sound not found!");
            }

            if (Mathf.Approximately(weaponSettings.fireRate, 0f))
            {
                Debug.LogWarning("FPSWeapon: Fire Rate is ZERO, setting it to default 600.");
                weaponSettings.fireRate = 600f;
            }

            AnimationClip idlePose = null;

            // 애니메이션 클립들을 뒤져서 재장전 시간, 무기 넣는 시간 등을 미리 계산해둡니다.
            foreach (var clip in weaponSettings.characterController.animationClips)
            {
                if (clip.name.Contains("Reload"))
                {
                    if (clip.name.Contains("Tac")) tacReloadDelay = clip.length;
                    if (clip.name.Contains("Empty")) emptyReloadDelay = clip.length;
                    continue;
                }

                if (clip.name.ToLower().Contains("unequip"))
                {
                    unEquipDelay = clip.length;
                    continue;
                }

                if (idlePose != null) continue;
                if (clip.name.Contains("Idle") || clip.name.Contains("Pose")) idlePose = clip;
            }

            if (idlePose != null)
            {
                idlePose.SampleAnimation(ownerPlayer, 0f);
            }

            cameraAnimator = owner.transform.parent.GetComponentInChildren<FPSCameraAnimator>();
        }

        // [재장전 함수]
        // R키를 눌렀을 때 호출됩니다.
        // 탄약이 남아있을 때(Tactical Reload)와 비었을 때(Empty Reload)를 구분하여 애니메이션을 재생합니다.
        public virtual void OnReload()
        {
            if (_activeAmmo == weaponSettings.ammo) return; // 이미 탄이 꽉 차있으면 무시

            var reloadHash = _activeAmmo == 0 ? RELOAD_EMPTY : RELOAD_TAC;
            characterAnimator.Play(reloadHash, -1, 0f);
            weaponAnimator.Play(reloadHash, -1, 0f);

            // 애니메이션 길이만큼 기다린 후 실제 탄약 숫자를 채워주는 함수(ResetActiveAmmo)를 예약 실행합니다.
            float delay = _activeAmmo == 0 ? emptyReloadDelay : tacReloadDelay;
            Invoke(nameof(ResetActiveAmmo), delay * weaponSettings.ammoResetTimeScale);
            _isReloading = true;

            // [추가] 플레이어에게 "나 재장전 중이야! 속도 줄여!" 라고 알림
            if (playerScript != null) playerScript.isReloading = true;
        }

        // [조정간 변경 함수]
        // 단발(Semi) <-> 연사(Auto) 모드를 변경하고, 반동 시스템에도 이를 알려줍니다.
        public void OnFireModeChange()
        {
            fireMode = fireMode == FireMode.Auto ? FireMode.Semi : weaponSettings.fullAuto ? FireMode.Auto : FireMode.Semi;
            recoilAnimation.fireMode = fireMode;
        }

        // [즉시 장착 함수]
        // 게임 시작 시 등 애니메이션 없이 바로 무기를 들고 있어야 할 때 사용합니다.
        public void OnEquipped_Immediate()
        {
            characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
            weaponAnimator.Play(IDLE, -1, 0f);
            recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);
        }

        // [장착 함수]
        // 무기를 꺼내는 애니메이션(Equip)을 재생합니다.
        // fastEquip이 true면 더 빠르게 꺼내거나 다른 모션을 취합니다.
        public void OnEquipped(bool fastEquip = false)
        {
            characterAnimator.runtimeAnimatorController = weaponSettings.characterController;
            recoilAnimation.Init(weaponSettings.recoilAnimData, weaponSettings.fireRate, fireMode);

            // 기본 포즈를 Idle로 초기화
            characterAnimator.Play(IDLE, -1, 0f);

            // 장착 애니메이션 재생
            if (weaponSettings.hasEquipOverride)
            {
                characterAnimator.Play("IKMovement", -1, 0f);
                characterAnimator.Play(fastEquip ? EQUIP : EQUIP_OVERRIDE, -1, 0f);
                return;
            }

            characterAnimator.Play(EQUIP, -1, 0f);
        }

        // [무기 해제 함수]
        // 다른 무기로 바꿀 때 호출됩니다. 무기를 집어넣는(UnEquip) 애니메이션을 재생하고
        // 무기를 완전히 집어넣는 데 걸리는 시간을 반환합니다.
        public float OnUnEquipped()
        {
            characterAnimator.SetTrigger(UNEQUIP);
            return unEquipDelay + 0.05f;
        }

        // [발사 버튼 누름]
        // 마우스 왼쪽 버튼을 눌렀을 때 호출됩니다. 사격 상태를 켜고 발사 로직을 시작합니다.
        public void OnFirePressed()
        {
            _isFiring = true;
            OnFire();
        }

        // [발사 버튼 뗌]
        // 마우스 왼쪽 버튼을 뗐을 때 호출됩니다. 사격 상태를 끄고 반동을 멈춥니다.
        public void OnFireReleased()
        {
            _isFiring = false;
            recoilAnimation.Stop();
        }

        // [실제 발사 로직]
        // 탄약 확인, 반동 재생, 사운드 재생, 카메라 흔들림, 탄약 감소 등을 처리합니다.
        // 연사(Auto) 모드일 경우 스스로를 재귀 호출(Invoke)하여 계속 발사합니다.
        // **총알 발사(Raycast) 로직을 추가한다면 이 함수 안에 넣어야 합니다.**
        private void OnFire()
        {
            if (!_isFiring || _isReloading) return; // 쏘는 중이 아니거나 재장전 중이면 취소

            if (_activeAmmo == 0)
            {
                OnFireReleased(); // 총알 없으면 사격 중지
                return;
            }

            // 1. 반동, 소리, 카메라 흔들림 재생
            recoilAnimation.Play();
            if (weaponSound != null) weaponSound.PlayFireSound();
            if (cameraAnimator != null) cameraAnimator.PlayCameraShake(weaponSettings.cameraShake);

            // 2. 캐릭터 및 무기 발사 애니메이션 재생
            if (weaponSettings.useFireClip) characterAnimator.Play(FIRE, -1, 0f);
            weaponAnimator.Play(weaponSettings.hasFireOut && _activeAmmo == 1
                ? FIREOUT // 마지막 한 발 남았을 때의 전용 모션 (슬라이드 스톱 등)
                : FIRE, -1, 0f);

            // 3. 탄약 감소
            _activeAmmo--;

            // 4. 연사 모드 처리 (단발이면 여기서 끝, 연사면 지정된 발사 속도만큼 기다렸다가 다시 OnFire 호출)
            if (fireMode == FireMode.Semi) return;
            Invoke(nameof(OnFire), 60f / weaponSettings.fireRate);
        }

        // [탄약 보충]
        // 재장전 애니메이션이 끝나는 시점에 호출되어 실제 탄창 숫자를 채웁니다.
        protected void ResetActiveAmmo()
        {
            _activeAmmo = weaponSettings.ammo;
            _isReloading = false;
            
            // [추가] 플레이어에게 "재장전 끝! 원래 속도로 가!" 라고 알림
            if (playerScript != null) playerScript.isReloading = false;
        }

        // [UI용] 현재 남은 탄약 수를 반환합니다.
        public int GetActiveAmmo()
        {
            return _activeAmmo;
        }

        // [UI용] 최대 탄약 용량을 반환합니다.
        public int GetMaxAmmo()
        {
            return weaponSettings.ammo;
        }
    }
}