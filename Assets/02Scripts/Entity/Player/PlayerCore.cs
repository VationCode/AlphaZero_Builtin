using Alpha.AlphaCamera;
using Alpha.Player.Inventory;
using Alpha.Mouse;
using Alpha.Player.Locomotion;
using Alpha.UI;
using UnityEngine;

namespace Alpha.Player
{
    public class PlayerCore : MonoBehaviour
    {
        #region ========== OutSideBind
        public AlphaInputSystem Input { get; private set; }
        public CameraCore CameraCore { get; private set; }
        public UIManager UIManager { get; private set; }
        public ResourceLoadSystem ResourceLoader { get; private set; }
        public DamageSystem DamageSystem { get; private set; }
        public MouseSystem MouseSystem { get; private set; }
        #endregion

        #region ========== Flow
        public LocomotionModeFlow LocomotionModeFlow { get; private set; }

        public ItemPickupFlow ItemPickupFlow { get; private set; }
        public PlayerInventoryFlow InventoryFlow { get; private set; }

        #endregion

        #region ========== Domain
        public LocomotionContext LocomotionContext { get; } = new();
        
        #endregion

        #region ========== Module
        public PlayerLocomotionModule LocomotionModule { get; private set; }
        public PlayerInventoryModule InventoryModule { get; private set; }
        //public PlayerEquipmentModule EquipmentModule { get; private set; }
        #endregion

        #region ========== View
        public PlayerAnimationView AnimationView { get; private set; }
        //public PlayerEquipmentView EquipmentView { get; private set; }
        #endregion
        public Transform PlayerTr;

        public bool CanLocomotion => _canLocomotion;
        private bool _canLocomotion;

        public bool BlockCombat => _isCombatBlocked;
        private bool _isCombatBlocked;

        public void Bind(AlphaInputSystem p_input, UIManager p_ui, CameraCore p_camera,
                         ResourceLoadSystem p_resourceLoad, DamageSystem p_damageSystem,
                        MouseSystem p_mouseSystem)
        {
            Input = p_input;
            UIManager = p_ui;
            CameraCore = p_camera;
            ResourceLoader = p_resourceLoad;
            DamageSystem = p_damageSystem;
            MouseSystem = p_mouseSystem;

        }

        private void Awake()
        {
            // Flow
            LocomotionModeFlow = GetComponentInChildren<LocomotionModeFlow>();
            ItemPickupFlow = GetComponent<ItemPickupFlow>();
            InventoryFlow = GetComponentInChildren<PlayerInventoryFlow>();
            //EquipmentFlow = GetComponentInChildren<PlayerEquipmentFlow>();

            // Module
            LocomotionModule = GetComponentInChildren<PlayerLocomotionModule>();
            InventoryModule = GetComponentInChildren<PlayerInventoryModule>();
            //EquipmentModule = GetComponentInChildren<PlayerEquipmentModule>();

            // View
            AnimationView = GetComponent<PlayerAnimationView>();

            PlayerTr = this.transform;
        }

        private void Start()
        {
            LocomotionModeFlow.Bind(this);
            LocomotionModule.Bind(LocomotionContext);

            ItemPickupFlow.Bind(InventoryModule);

            //EquipmentFlow.Bind(this);

            AnimationView.Bind(PlayerTr);
        }

        private void Update()
        {

        }

        public void SetCombatBlocked(bool p_isBlocked)
        {
            _isCombatBlocked = p_isBlocked;
        }

    }
}