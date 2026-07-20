using Alpha.AlphaCamera;
using Alpha.Mouse;
using UnityEngine;

namespace Alpha.Player
{
    //[RequireComponent(typeof(LocomotionMotorModule), typeof(GroundLocomotionModule), typeof(LocomotionModeFlow))]
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
        //public LocomotionModeFlow LocomotionModeFlow { get; private set; }

        public ItemPickupFlow ItemPickupFlow { get; private set; }
        public PlayerInventoryFlow InventoryFlow { get; private set; }

        public PlayerEquipmentFlow EquipmentFlow { get; private set; }
        #endregion

        #region ========== Domain
        //public PlayerContext Context { get; } = new PlayerContext();
        #endregion

        #region ========== Module
        //public LocomotionMotorModule LocomotionMotor { get; private set; }
        //public GroundLocomotionModule GroundLocomotion { get; private set; }
        public PlayerInventoryModule InventoryModule { get; private set; }
        public PlayerEquipmentModule EquipmentModule { get; private set; }
        #endregion

        #region ========== View
        public PlayerAnimationView AnimationView { get; private set; }
        public PlayerEquipmentView EquipmentView { get; private set; }
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
           //LocomotionModeFlow = GetComponent<LocomotionModeFlow>();
            ItemPickupFlow = GetComponent<ItemPickupFlow>();
            InventoryFlow = GetComponent<PlayerInventoryFlow>();
            EquipmentFlow = GetComponent<PlayerEquipmentFlow>();

            // Module
            //LocomotionMotor = GetComponent<LocomotionMotorModule>();
            //GroundLocomotion = GetComponent<GroundLocomotionModule>();
            InventoryModule = GetComponent<PlayerInventoryModule>();
            EquipmentModule = GetComponent<PlayerEquipmentModule>();

            // View
            AnimationView = GetComponent<PlayerAnimationView>();
            EquipmentView = GetComponent<PlayerEquipmentView>();

            PlayerTr = this.transform;
        }

        private void Start()
        {
            // 외부 Camera를 Motor의 이동 기준으로 연결한다.
            //LocomotionMotor.Bind(CameraCore.RenderCamera.transform);

            // Ground Module에 공통 Motor를 연결한다.
            //GroundLocomotion.Bind(LocomotionMotor);

            // 모든 Module 연결 후 Locomotion Flow를 시작한다.
            //LocomotionModeFlow.Bind(this);

            ItemPickupFlow.Bind(InventoryModule);

            EquipmentFlow.Bind(this);


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