using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player.Animation;
using Alpha.Player.Locomotion;
using Alpha.Player.Inventory;
using Alpha.Player.Equipment;
using Alpha.Player.Combat;
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
        //public PlayerEquipmentView EquipmentView { get; private set; }

        #endregion

        public Transform PlayerTr {  get; private set; }

        public bool CanLocomotion => _canLocomotion;
        private bool _canLocomotion;

        public bool BlockCombat => _isCombatBlocked;
        private bool _isCombatBlocked;

        // Installer가 소유한 입력·카메라·마우스·데이터 참조를 전달받는다.
        public void Bind(AlphaInputSystem p_input, CameraCore p_camera, MouseSystem p_mouseSystem, ItemDatabaseManager p_itemDatabase)
        {
            Input = p_input;
            CameraCore = p_camera;
            MouseSystem = p_mouseSystem;
            ItemDatabase = p_itemDatabase;
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

            PlayerTr = this.transform;
        }

        // Player 내부 Feature를 Context와 실행 순서에 맞춰 초기화한다.
        private void Start()
        {
            // 이동 상태와 실제 이동 Module을 먼저 구성한다.
            LocomotionModeFlow.Bind(this);

            LocomotionModule.Bind(LocomotionContext, PlayerTr);

            // 인벤토리 슬롯 생성 후 입력 Flow를 연결한다.
            InventoryModule.Initialize(InventoryContext);
            InventoryFlow.Bind(InventoryContext, InventoryModule, Input);

            // 장비 상태와 인벤토리 간 이동 Flow를 연결한다.
            EquipmentModule.Bind(EquipmentContext);
            EquipmentFlow.Bind(EquipmentModule, InventoryContext, InventoryModule);

            // 픽업과 애니메이션은 앞에서 준비된 Player 기능을 사용한다.
            ItemPickupFlow.Bind(InventoryModule, ItemDatabase);

            //CombatModule.Bind(EquipmentContext);

            AnimationView.Bind(PlayerTr);


        }

        // 다른 행동이 전투 입력을 막는 상태인지 기록한다.
        public void SetCombatBlocked(bool p_isBlocked)
        {
            _isCombatBlocked = p_isBlocked;
        }

        /// <summary>
        /// Player 내부 Combat Module을 조립한다.
        /// Equipment 연결이 완료된 후 호출해야 한다.
        /// </summary>
        public bool BindCombat()
        {
            if (CombatModule == null)
            {
                Debug.LogError($"{nameof(Combat.CombatModule)}을 찾을 수 없습니다.", this);
                return false;
            }

            return CombatModule.Bind(this);
        }

        // 현재 PlayerCore가 직접 구독하는 해제 대상은 없다.
        private void OnDestroy()
        {

        }
    }
}
