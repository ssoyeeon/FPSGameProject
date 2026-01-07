// Designed by KINEMATION, 2025.

using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KINEMATION.FPSAnimationPack.Scripts.Player
{
    [Serializable]
    public struct IKTransforms
    {
        public Transform tip;
        public Transform mid;
        public Transform root;

        [NonSerialized] public TransformStreamHandle tipHandle;
        [NonSerialized] public TransformStreamHandle midHandle;
        [NonSerialized] public TransformStreamHandle rootHandle;

        // 애니메이터의 스트림 핸들을 초기화하여 IK 연산에 사용할 준비를 합니다.
        public void Initialize(Animator animator)
        {
            tipHandle = animator.BindStreamTransform(tip);
            midHandle = animator.BindStreamTransform(mid);
            rootHandle = animator.BindStreamTransform(root);
        }
    }
    
    [AddComponentMenu("KINEMATION/FPS Animation Pack/Character/FPS Player")]
    public class FPSPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float ikWeight = 1f;
        public float AdsWeight => _adsWeight;
        
        public FPSPlayerSettings playerSettings;

        [HideInInspector] public bool isReloading = false;

        [Header("Skeleton")]
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Transform weaponBone;
        [SerializeField] private Transform weaponBoneAdditive;
        [SerializeField] private Transform cameraPoint;
        [SerializeField] private IKTransforms rightHand;
        [SerializeField] private IKTransforms leftHand;
        
        private KTwoBoneIkData _rightHandIk;
        private KTwoBoneIkData _leftHandIk;
        
        private RecoilAnimation _recoilAnimation;
        private float _adsWeight;

        private List<FPSWeapon> _weapons = new List<FPSWeapon>();
        private List<FPSWeapon> _prefabComponents = new List<FPSWeapon>();
        private int _activeWeaponIndex = 0;

        private Animator _animator;

        // 애니메이터 파라미터 해시값 캐싱 (성능 최적화)
        private static int RIGHT_HAND_WEIGHT = Animator.StringToHash("RightHandWeight");
        private static int TAC_SPRINT_WEIGHT = Animator.StringToHash("TacSprintWeight");
        private static int GRENADE_WEIGHT = Animator.StringToHash("GrenadeWeight");
        private static int THROW_GRENADE = Animator.StringToHash("ThrowGrenade");
        private static int GAIT = Animator.StringToHash("Gait");
        private static int IS_IN_AIR = Animator.StringToHash("IsInAir");
        private static int INSPECT = Animator.StringToHash("Inspect");
        
        private int _tacSprintLayerIndex;
        private int _triggerDisciplineLayerIndex;
        private int _rightHandLayerIndex;
        
        private bool _isAiming;

        private Vector2 _moveInput;
        private float _smoothGait;

        private Vector2 _lookInput;

        private bool _bSprinting;
        private bool _bTacSprinting;

        private FPSPlayerSound _playerSound;

        private KTransform _localCameraPoint;
        private CharacterController _controller;

        private FPSProceduralJob _job;
        private AnimationScriptPlayable _playable;

        public Texture2D defaultCrosshair;          // 기본 크로스헤어 텍스처
        private Color crosshairColor = Color.white;  // 크로스헤어 색상
        private float crosshairSize = 25f;           // 크로스헤어 크기


        [Header("Attack")]
        [SerializeField] private float range = 3.0f;
        [SerializeField] private float force = 8.0f;
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Aim")]
        [Tooltip("비우면 main Camera 사용")]
        [SerializeField] private Transform aimOrigin;

        // 현재 무기를 비활성화하고 다음 인덱스의 무기를 장착합니다 (무기 순환).
        private void EquipWeapon_Incremental()
        {
            GetActiveWeapon().gameObject.SetActive(false);
            _activeWeaponIndex = _activeWeaponIndex + 1 > _weapons.Count - 1 ? 0 : _activeWeaponIndex + 1;
            EquipWeapon();
        }
        
        // 실제 무기를 장착하는 로직을 처리합니다.
        // fastEquip: 애니메이션 없이 빠르게 장착할지 여부
        // equipImmediately: 딜레이 없이 즉시 장착 로직을 수행할지 여부
        private void EquipWeapon(bool fastEquip = false, bool equipImmediately = false)
        {
            if (equipImmediately)
            {
                GetActiveWeapon().OnEquipped_Immediate();
            }
            else
            {
                GetActiveWeapon().OnEquipped(fastEquip);
            }

            // 절차적 애니메이션 Job에 현재 무기의 데이터(손 위치, 조준 위치 등)를 전달합니다.
            _job.defaultRightHandPose = GetActiveWeapon().rightHandPose;
            _job.additiveAdsPose = GetActiveWeapon().adsPose;
            _job.gunSettings = GetActiveWeapon().weaponSettings;
            _job.aimPointTransform = GetActiveWeapon().aimPoint;
            _playable.SetJobData(_job);

            if (equipImmediately) return;
            
            // 약간의 딜레이 후 무기 모델을 보이게 설정합니다.
            Invoke(nameof(SetWeaponVisible), 0.05f);
        }

        // 수류탄 투척 후 등, 무기를 빠르게 다시 꺼낼 때 사용합니다.
        private void FastEquipWeapon()
        {
            EquipWeapon(true);
        }

        // 수류탄 투척 애니메이션 실행 후, 일정 시간 뒤에 무기를 다시 장착하도록 예약합니다.
        private void ThrowGrenade()
        {
            GetActiveWeapon().gameObject.SetActive(false);
            Invoke(nameof(FastEquipWeapon), playerSettings.grenadeDelay);
        }

        // 착지 시 애니메이터의 공중 상태(IsInAir)를 해제합니다.
        private void OnLand()
        {
            _animator.SetBool(IS_IN_AIR, false);
        }

        // 수류탄 투척 키 입력 시 호출됩니다. 투척 트리거를 당기고 무기 해제를 시작합니다.
        public void OnThrowGrenade()
        {
            _animator.SetTrigger(THROW_GRENADE);
            Invoke(nameof(ThrowGrenade), GetActiveWeapon().UnEquipDelay);
        }

        // 무기 교체 키 입력 시 호출됩니다. 현재 무기를 집어넣고 다음 무기를 꺼냅니다.
        public void OnChangeWeapon()
        {
            if (_weapons.Count <= 1) return;
            float delay = GetActiveWeapon().OnUnEquipped();
            Invoke(nameof(EquipWeapon_Incremental), delay);
        }

        // 조정간(단발/연사) 변경 키 입력 시 호출됩니다. 소리를 재생하고 IK 모션을 적용합니다.
        public void OnChangeFireMode()
        {
            var prevFireMode = GetActiveWeapon().ActiveFireMode;
            GetActiveWeapon().OnFireModeChange();

            if (prevFireMode != GetActiveWeapon().ActiveFireMode)
            {
                _playerSound.PlayFireModeSwitchSound();
                _job.PlayIkMotion(playerSettings.fireModeMotion);
            }
        }
        
        // 재장전 키 입력 시 호출됩니다.
        public void OnReload()
        {
            GetActiveWeapon().OnReload();
        }
        
        // 점프 키 입력 시 호출됩니다. 공중 상태로 설정하고 착지 로직을 예약합니다.
        public void OnJump()
        {
            _animator.SetBool(IS_IN_AIR, true);
            Invoke(nameof(OnLand), 0.4f);
        }
        
        // 무기 살펴보기(Inspect) 키 입력 시 호출됩니다.
        public void OnInspect()
        {
            _animator.CrossFade(INSPECT, 0.1f);
        }
        
#if ENABLE_INPUT_SYSTEM
        // 마우스 휠 입력 처리: 무기를 이전/다음으로 교체합니다.
        public void OnMouseWheel(InputValue value)
        {
            float mouseWheelValue = value.Get<float>();
            if (mouseWheelValue == 0f) return;
            
            GetActiveWeapon().gameObject.SetActive(false);
            
            _activeWeaponIndex += mouseWheelValue > 0f ? 1 : -1;

            if (_activeWeaponIndex < 0) _activeWeaponIndex = _weapons.Count - 1;
            if(_activeWeaponIndex > _weapons.Count - 1) _activeWeaponIndex = 0;
            
            GetActiveWeapon().gameObject.SetActive(true);
            EquipWeapon(false, true);
        }
        
        // 발사 버튼 입력 처리: 누름/뗌 상태에 따라 무기의 발사 로직을 호출합니다.
        public void OnFire(InputValue value)
        {
            if(value.isPressed)
            {
                Ray ray = new Ray(cameraPoint.position, cameraPoint.transform.forward);

                if(Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.TryGetComponent<IHittable>(out var hittable))
                    {
                        Vector3 dir = (hit.point - ray.origin).normalized;

                        var info = new HitInfo(

                            point: hit.point,
                            normal: hit.normal,
                            direction: dir,
                            force: force,
                            damage: damage,
                            attacker: gameObject
                        );

                        hittable.OnHit(in info);
                        return;
                    }
                }
                GetActiveWeapon().OnFirePressed();
                return;
            }
            
            GetActiveWeapon().OnFireReleased();
        }

        // 조준(Zoom) 버튼 입력 처리: 조준 상태를 토글하고 사운드 및 IK 모션을 실행합니다.
        public void OnAim(InputValue value)
        {
            bool wasAiming = _isAiming;
            _isAiming = value.isPressed;
            _recoilAnimation.isAiming = _isAiming;

            if (wasAiming != _isAiming)
            {
                _playerSound.PlayAimSound(_isAiming);
                _job.PlayIkMotion(playerSettings.aimingMotion);
            }
        }

        // 이동 입력(WASD) 값을 받아옵니다.
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        // 달리기(Sprint) 입력 처리
        public void OnSprint(InputValue value)
        {
            _bSprinting = value.isPressed;
            if(!_bSprinting) _bTacSprinting = false;
        }
        
        // 전술적 달리기(TacSprint) 입력 처리
        public void OnTacSprint(InputValue value)
        {
            if (!_bSprinting) return;
            _bTacSprinting = value.isPressed;
        }

        // 마우스 시점 회전 입력 값을 받아옵니다.
        public void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>() * playerSettings.sensitivity;
            _lookInput.y = Mathf.Clamp(_lookInput.y - input.y, -90f, 90f);
            _lookInput.x = input.x;
        }
#endif
#if !ENABLE_INPUT_SYSTEM
        // 레거시 입력 시스템용 시점 회전 처리
        private void OnLookLegacy()
        {
            Vector2 input = new Vector2()
            {
                x = Input.GetAxis("Mouse X"),
                y = Input.GetAxis("Mouse Y")
            };
            
            _lookInput.y = Mathf.Clamp(_lookInput.y - input.y, -90f, 90f);
            _lookInput.x = input.x;
        }

        // 레거시 입력 시스템용 마우스 휠 처리
        private void OnMouseWheelLegacy()
        {
            float mouseWheelValue = Input.GetAxis("Mouse ScrollWheel");
            if (mouseWheelValue == 0f) return;
            
            GetActiveWeapon().gameObject.SetActive(false);
            _activeWeaponIndex += mouseWheelValue > 0f ? 1 : -1;

            if (_activeWeaponIndex < 0) _activeWeaponIndex = _weapons.Count - 1;
            if(_activeWeaponIndex > _weapons.Count - 1) _activeWeaponIndex = 0;
            
            GetActiveWeapon().gameObject.SetActive(true);
            GetActiveWeapon().OnEquipped_Immediate();
        }

        // 레거시 입력 시스템용 조준 처리
        private void OnAimLegacy(bool isPressed)
        {
            bool wasAiming = _isAiming;
            _isAiming = isPressed;
            _recoilAnimation.isAiming = _isAiming;
            
            if(wasAiming != _isAiming) 
            {
                _playerSound.PlayAimSound(_isAiming);
                _job.PlayIkMotion(playerSettings.aimingMotion);
            }
        }
        
        // 레거시 입력 시스템용 이동 처리
        private void OnMoveLegacy()
        {
            _moveInput.x = Input.GetAxis("Horizontal");
            _moveInput.y = Input.GetAxis("Vertical");
            _moveInput.Normalize();
        }

        // 레거시 입력 시스템용 달리기 처리
        private void OnSprintLegacy(bool isPressed)
        {
            _bSprinting = isPressed;
            if(!_bSprinting) _bTacSprinting = false;
        }

        // 레거시 입력 시스템용 전술 달리기 처리
        private void OnTacSprintLegacy(bool isPressed)
        {
            if (!_bSprinting) return;
            _bTacSprinting = isPressed;
        }
        
        // 레거시 입력들을 통합해서 매 프레임 체크하는 함수
        private void ProcessLegacyInputs()
        {
            OnMouseWheelLegacy();
            if (Input.GetKeyDown(KeyCode.G)) OnThrowGrenade();
            if (Input.GetKeyDown(KeyCode.F)) OnChangeWeapon();
            if (Input.GetKeyDown(KeyCode.B)) OnChangeFireMode();
            if (Input.GetKeyDown(KeyCode.R)) OnReload();
            if (Input.GetKeyDown(KeyCode.Space)) OnJump();
            if (Input.GetKeyDown(KeyCode.I)) OnInspect();
            
            if (Input.GetKeyDown(KeyCode.Mouse0)) GetActiveWeapon().OnFirePressed();
            if (Input.GetKeyUp(KeyCode.Mouse0)) GetActiveWeapon().OnFireReleased();

            OnAimLegacy(Input.GetKey(KeyCode.Mouse1));
            OnMoveLegacy();
            OnLookLegacy();
            OnSprintLegacy(Input.GetKey(KeyCode.LeftShift));
            OnTacSprintLegacy(Input.GetKey(KeyCode.X));
        }
#endif
        // 무기 게임 오브젝트를 활성화하여 보이게 합니다.
        private void SetWeaponVisible()
        {
            GetActiveWeapon().gameObject.SetActive(true);
        }

        // 현재 활성화된 무기 컴포넌트를 반환합니다.
        public FPSWeapon GetActiveWeapon()
        {
            return _weapons[_activeWeaponIndex];
        }

        // 현재 무기의 프리팹 원본 정보를 반환합니다.
        public FPSWeapon GetActivePrefab()
        {
            return _prefabComponents[_activeWeaponIndex];
        }

        // 초기화 함수: 마우스 설정, 컴포넌트 할당, 절차적 애니메이션 Job 설정, 무기 생성 등을 수행합니다.
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;   //마우스 잠금 
            Cursor.visible = false; // 마우스 커서 숨김


            _animator = GetComponent<Animator>();
            _controller = transform.root.GetComponent<CharacterController>();
            _recoilAnimation = GetComponent<RecoilAnimation>();
            _playerSound = GetComponent<FPSPlayerSound>();

            // 절차적 애니메이션(IK 등)을 위한 Job 설정
            _job = new FPSProceduralJob()
            {
                animator = _animator,
                skeletonRoot = skeletonRoot,
                rightArm = rightHand,
                leftArm = leftHand,
                weaponBone = weaponBone,
                weaponBoneAdditive = weaponBoneAdditive,
                cameraSocket = cameraPoint,
                recoilAnimation = _recoilAnimation
            };
            _job.Setup();
            
            // Playable Graph 생성 및 연결 (애니메이션 시스템에 커스텀 로직 주입)
            _playable = AnimationScriptPlayable.Create(_animator.playableGraph, _job);
            var output = AnimationPlayableOutput.Create(_animator.playableGraph, "FPS Procedural Output", 
                _animator);
            
            output.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
            output.SetSourcePlayable(_playable);
            
            // 애니메이터 레이어 인덱스 캐싱
            _triggerDisciplineLayerIndex = _animator.GetLayerIndex("TriggerDiscipline");
            _rightHandLayerIndex = _animator.GetLayerIndex("RightHand");
            _tacSprintLayerIndex = _animator.GetLayerIndex("TacSprint");
            
            KTransform root = new KTransform(transform);
            _localCameraPoint = root.GetRelativeTransform(new KTransform(cameraPoint), false);

            // 설정된 무기 프리팹들을 인스턴스화하고 리스트에 추가합니다.
            foreach (var prefab in playerSettings.weaponPrefabs)
            {
                var prefabComponent = prefab.GetComponent<FPSWeapon>();
                if(prefabComponent == null) continue;
                
                _prefabComponents.Add(prefabComponent);
                
                var instance = Instantiate(prefab, weaponBone, false);
                instance.SetActive(false);
                
                var component = instance.GetComponent<FPSWeapon>();
                component.Initialize(gameObject);

                KTransform weaponT = new KTransform(weaponBone);
                component.rightHandPose = new KTransform(rightHand.tip).GetRelativeTransform(weaponT, false);
                
                var localWeapon = root.GetRelativeTransform(weaponT, false);

                localWeapon.rotation *= prefabComponent.weaponSettings.rotationOffset;
                
                component.adsPose.position = _localCameraPoint.position - localWeapon.position;
                component.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);

                _weapons.Add(component);
            }

            EquipWeapon();
        }

        // 현재 상태(달리기, 걷기 등)에 따른 목표 속도(Gait) 값을 계산합니다.
        private float GetDesiredGait()
        {
            if (_bTacSprinting) return 3f;
            if (_bSprinting) return 2f;
            return _moveInput.magnitude;
        }
        
        // 매 프레임 호출: 입력 처리, 애니메이션 상태 업데이트, 캐릭터 이동 등을 수행합니다.
        private void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            ProcessLegacyInputs();
#endif
            // 조준(ADS) 가중치를 부드럽게 보간합니다.
            _adsWeight = Mathf.Clamp01(_adsWeight + playerSettings.aimSpeed * Time.deltaTime * (_isAiming ? 1f : -1f));

            // 이동 속도(Gait) 값을 부드럽게 보간합니다.
            _smoothGait = Mathf.Lerp(_smoothGait, GetDesiredGait(), 
                KMath.ExpDecayAlpha(playerSettings.gaitSmoothing, Time.deltaTime));
            
            // 애니메이터에 계산된 파라미터들을 전달합니다.
            _animator.SetFloat(GAIT, _smoothGait);
            _animator.SetLayerWeight(_tacSprintLayerIndex, Mathf.Clamp01(_smoothGait - 2f));

            // 달리기 중 사격 금지(Trigger Discipline) 처리
            bool triggerAllowed = GetActiveWeapon().weaponSettings.useSprintTriggerDiscipline;
            _animator.SetLayerWeight(_triggerDisciplineLayerIndex,
                triggerAllowed ? _animator.GetFloat(TAC_SPRINT_WEIGHT) : 0f);

            _animator.SetLayerWeight(_rightHandLayerIndex, _animator.GetFloat(RIGHT_HAND_WEIGHT));
            
            // 절차적 애니메이션 Job 데이터 업데이트
            _job.Update();
            _job.adsWeight = _adsWeight;
            _job.ikWeight = ikWeight;
            _playable.SetJobData(_job);
            
            // 카메라 및 캐릭터 회전 처리
            Vector3 cameraPosition = -_localCameraPoint.position;
            
            transform.localRotation = Quaternion.Euler(_lookInput.y, 0f, 0f);
            transform.localPosition = transform.localRotation * cameraPosition - cameraPosition;

            // 캐릭터 컨트롤러를 이용한 실제 이동 처리
            if (_controller != null)
            {
                Transform root = _controller.transform;
                root.rotation *= Quaternion.Euler(0f, _lookInput.x, 0f);
                Vector3 movement = root.forward * _moveInput.y + root.right * _moveInput.x;
                
                // [수정] 원래 코드 뒤에 속도 페널티 로직 추가
                // isReloading이 true면 0.5(절반 속도), 아니면 1(정상 속도)을 곱함
                float speedMultiplier = isReloading ? 0.5f : 1f;

                movement *= _smoothGait * 1.5f * speedMultiplier; // 여기에 곱해주기!

                _controller.Move(movement * Time.deltaTime);
            }
        }

        // (현재 사용되지 않는 것으로 보임, Animation Event로 호출될 가능성 있음) 반동 애니메이션 재생
        private void OnFire()
        {
            _recoilAnimation.Play();
        }

        // GUI 그리기: 화면 중앙에 크로스헤어를 그립니다.
        // OnGUI는 Unity의 구형 UI 시스템으로, 성능상 추천되지 않으나 간단한 테스트용으로는 작동합니다.
        void OnGUI()
        {
            // 화면 중앙 좌표 계산
            float centerX = Screen.width / 2;
            float centerY = Screen.height / 2;

            // 크로스헤어 위치 및 크기 계산
            Rect crosshairRect = new Rect(centerX - crosshairSize / 2, centerY - crosshairSize / 2, crosshairSize, crosshairSize);

            // 크로스헤어 그리기
            GUI.color = crosshairColor;
            GUI.DrawTexture(crosshairRect, defaultCrosshair);
        }
    }
}