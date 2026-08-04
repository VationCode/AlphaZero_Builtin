using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player.Animation;
using Alpha.Player.Combat;
using Alpha.Player.Inventory;

//using Alpha.Player.Equipment;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player
{
    public class PlayerCore : MonoBehaviour
    {
        #region ========== OutSideBind
        public AlphaInputSystem Input { get; private set; }
        public CameraCore CameraCore { get; private set; }
        public MouseSystem MouseSystem { get; private set; }
        #endregion

        #region ========== Flow
        public LocomotionModeFlow LocomotionModeFlow { get; private set; }
        public InventoryFlow InventoryFlow { get; private set; }
        public CombatFlow CombatFlow { get; private set; }
        public ItemPickupFlow ItemPickupFlow { get; private set; }
        //public PlayerEquipmentFlow EquipmentFlow { get; } = new();
        #endregion

        #region ========== Domain
        public LocomotionContext LocomotionContext { get; } = new();
        public InventoryContext InventoryContext { get; } = new();
        // Player 생명주기 동안 하나의 현재 무기 상태를 유지한다.
        //public PlayerEquipmentContext EquipmentContext { get; } = new();

        // Combat State들이 공유하는 상태는 Player 생명주기 동안 유지한다.
        public CombatContext CombatContext { get; } = new();
        #endregion

        #region ========== Module
        public LocomotionModule LocomotionModule { get; private set; }
        public InventoryModule InventoryModule { get; private set; }
        //public PlayerEquipmentModule EquipmentModule { get; private set; }
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

        public void Bind(AlphaInputSystem p_input, CameraCore p_camera, MouseSystem p_mouseSystem)
        {
            Input = p_input;
            CameraCore = p_camera;
            MouseSystem = p_mouseSystem;
        }

        private void Awake()
        {
            // Flow
            LocomotionModeFlow = GetComponentInChildren<LocomotionModeFlow>(true);
            InventoryFlow = GetComponentInChildren<InventoryFlow>(true);
            CombatFlow = GetComponentInChildren<CombatFlow>(true);
            ItemPickupFlow = GetComponent<ItemPickupFlow>();

            // Module
            LocomotionModule = GetComponentInChildren<LocomotionModule>(true);
            InventoryModule = GetComponentInChildren<InventoryModule>(true);
            //EquipmentModule = GetComponentInChildren<PlayerEquipmentModule>(true);
            CombatModule = GetComponentInChildren<CombatModule>(true);

            // View
            AnimationView = GetComponent<PlayerAnimationView>();
            //EquipmentView = GetComponentInChildren<PlayerEquipmentView>();

            PlayerTr = this.transform;
        }

        private void Start()
        {
            LocomotionModeFlow.Bind(this);

            LocomotionModule.Bind(LocomotionContext, PlayerTr);

            InventoryModule.Initialize(InventoryContext);
            InventoryFlow.Bind(InventoryContext, InventoryModule, Input);

            //CombatModule.Bind(EquipmentContext);

            AnimationView.Bind(PlayerTr);


        }

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

        private void OnDestroy()
        {

        }
    }
}
