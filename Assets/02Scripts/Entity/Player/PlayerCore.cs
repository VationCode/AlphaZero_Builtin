using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player.Animation;
using Alpha.Player.Locomotion;
using Alpha.Player.Inventory;
using Alpha.Player.Equipment;
using Alpha.Player.Combat;
using Alpha.Item.Weapon;
using Alpha.Player.Audio;
using Alpha.Player.Effect;
using UnityEngine;

namespace Alpha.Player
{
    // PlayerCore 내부 기능을 조립하고 외부 진입점을 제공한다.
    public class PlayerCore : MonoBehaviour
    {
        #region ========== OutSideBind
        public AlphaInputSystem Input { get; private set; }
        public CameraCore CameraCore { get; private set; }
        public MouseSystem MouseSystem { get; private set; }
        public ItemDatabaseManager ItemDatabase { get; private set; }
        public ResourceLoadSystem ResourceLoader { get; private set; }
        #endregion

        #region ========== Flow
        public LocomotionModeFlow LocomotionModeFlow { get; private set; }
        public InventoryFlow InventoryFlow { get; private set; }
        public CombatFlow CombatFlow { get; private set; }
        public ItemPickupFlow ItemPickupFlow { get; private set; }
        public EquipmentFlow EquipmentFlow { get; private set; }
        #endregion

        #region ========== Domain
        public LocomotionContext LocomotionContext { get; } = new();
        public InventoryContext InventoryContext { get; } = new();
        public EquipmentContext EquipmentContext { get; } = new();

        // Combat State들이 공유하는 상태는 Player 생명주기 동안 유지한다.
        public CombatContext CombatContext { get; } = new();
        #endregion

        #region ========== Module
        public LocomotionModule LocomotionModule { get; private set; }
        public InventoryModule InventoryModule { get; private set; }
        public EquipmentModule EquipmentModule { get; private set; }
        public CombatModule CombatModule { get; private set; }
        #endregion

        #region ========== View
        public PlayerAnimationView AnimationView { get; private set; }
        public PlayerLocomotionAudioView LocomotionAudioView { get; private set; }
        public PlayerActionEffectView ActionEffectView { get; private set; }
        public PlayerArmorView ArmorView { get; private set; }
        public PlayerScopeView ScopeView { get; private set; }
        //public PlayerEquipmentView EquipmentView { get; private set; }

        #endregion

        public Transform PlayerTr {  get; private set; }

        public bool CanLocomotion => _canLocomotion;
        private bool _canLocomotion;

        public bool BlockCombat => _isCombatBlocked;
        private bool _isCombatBlocked;

        // Installer가 소유한 입력·카메라·마우스·데이터 참조를 전달받는다.
        public void Bind(
            AlphaInputSystem p_input,
            CameraCore p_camera,
            MouseSystem p_mouseSystem,
            ItemDatabaseManager p_itemDatabase,
            ResourceLoadSystem p_resourceLoader)
        {
            Input = p_input;
            CameraCore = p_camera;
            MouseSystem = p_mouseSystem;
            ItemDatabase = p_itemDatabase;
            ResourceLoader = p_resourceLoader;
        }

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            // Flow
            LocomotionModeFlow = GetComponentInChildren<LocomotionModeFlow>(true);
            InventoryFlow = GetComponentInChildren<InventoryFlow>(true);
            EquipmentFlow = GetComponentInChildren<EquipmentFlow>(true);
            CombatFlow = GetComponentInChildren<CombatFlow>(true);
            ItemPickupFlow = GetComponentInChildren<ItemPickupFlow>();

            // Module
            LocomotionModule = GetComponentInChildren<LocomotionModule>(true);
            InventoryModule = GetComponentInChildren<InventoryModule>(true);
            EquipmentModule = GetComponentInChildren<EquipmentModule>(true);
            CombatModule = GetComponentInChildren<CombatModule>(true);

            // View
            AnimationView = GetComponent<PlayerAnimationView>();
            LocomotionAudioView = GetComponent<PlayerLocomotionAudioView>();
            ActionEffectView = GetComponentInChildren<PlayerActionEffectView>(true);
            ArmorView = GetComponent<PlayerArmorView>();
            ScopeView = GetComponent<PlayerScopeView>();

            PlayerTr = this.transform;
        }

        // Player 내부 Feature를 Context와 실행 순서에 맞춰 초기화한다.
        private void Start()
        {
            // 이동 상태와 실제 이동 Module을 먼저 구성한다.
            LocomotionModeFlow.Bind(this);

            LocomotionModule.Bind(LocomotionContext, PlayerTr);

            // Inventory와 Equipment가 같은 슬롯 이동 규칙을 공유한다.
            SlotTransferModule slotTransferModule = new();

            // 인벤토리 슬롯 생성 후 입력 Flow를 연결한다.
            InventoryModule.Initialize(InventoryContext, slotTransferModule);
            InventoryFlow.Bind(InventoryContext, InventoryModule, Input);

            // 장비 상태와 인벤토리 간 이동 Flow를 연결한다.
            EquipmentModule.Bind(
                EquipmentContext,
                InventoryModule,
                slotTransferModule,
                ResourceLoader);
            EquipmentFlow.Bind(EquipmentModule, InventoryContext);
            ArmorView?.Bind(EquipmentContext, ResourceLoader);

            // 장비 변경 완료 이벤트를 실제 무기 생성 기능과 연결한다.
            if (CombatModule.Bind(this))
            {
                EquipmentFlow.OnWeaponChanged -= CombatFlow.HandleEquipmentWeaponChanged;
                EquipmentFlow.OnWeaponChanged += CombatFlow.HandleEquipmentWeaponChanged;

                CombatFlow.Bind(this);
            }

            // 픽업과 애니메이션은 앞에서 준비된 Player 기능을 사용한다.
            ItemPickupFlow.Bind(InventoryModule, ItemDatabase, Input);

            AnimationView.Bind(PlayerTr);
            LocomotionContext.OnStateChanged -=
                AnimationView.HandleLocomotionStateChanged;
            LocomotionContext.OnStateChanged +=
                AnimationView.HandleLocomotionStateChanged;

            if (LocomotionContext.CurrentState.HasValue)
            {
                AnimationView.HandleLocomotionStateChanged(
                    LocomotionContext.CurrentMode,
                    LocomotionContext.CurrentState.Value);
            }

            LocomotionAudioView?.Bind(LocomotionContext);
            ActionEffectView?.Bind(LocomotionContext);
            ScopeView?.Bind(CameraCore);
            AnimationView.OnRootMotion -= LocomotionModule.ApplyRootMotion;
            AnimationView.OnRootMotion += LocomotionModule.ApplyRootMotion;

            if (LocomotionAudioView != null)
            {
                AnimationView.OnFootstep -= LocomotionAudioView.PlayFootstep;
                AnimationView.OnFootstep += LocomotionAudioView.PlayFootstep;
            }

        }

        // 다른 행동이 전투 입력을 막는 상태인지 기록한다.
        public void SetCombatBlocked(bool p_isBlocked)
        {
            _isCombatBlocked = p_isBlocked;
        }

        // Player가 연결한 장비 변경 이벤트를 해제한다.
        private void OnDestroy()
        {
            if (EquipmentFlow != null && CombatModule != null)
                EquipmentFlow.OnWeaponChanged -= CombatFlow.HandleEquipmentWeaponChanged;

            if (AnimationView != null && LocomotionModule != null)
                AnimationView.OnRootMotion -= LocomotionModule.ApplyRootMotion;

            if (AnimationView != null)
            {
                LocomotionContext.OnStateChanged -=
                    AnimationView.HandleLocomotionStateChanged;
            }

            if (AnimationView != null && LocomotionAudioView != null)
                AnimationView.OnFootstep -= LocomotionAudioView.PlayFootstep;

            LocomotionAudioView?.Unbind();
            ActionEffectView?.Unbind();
            ArmorView?.Unbind();
            EquipmentModule?.Unbind();

            ScopeView?.Unbind();
        }
    }
}
