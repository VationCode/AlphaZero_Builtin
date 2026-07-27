using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player
{
    public class PlayerCore : MonoBehaviour
    {
        #region ========== OutSideBind
        public AlphaInputSystem Input { get; private set; }
        public CameraCore CameraCore { get; private set; }
        public DamageSystem DamageSystem { get; private set; }
        public MouseSystem MouseSystem { get; private set; }
        #endregion

        #region ========== Flow
        public LocomotionModeFlow LocomotionModeFlow { get; private set; }

        public ItemPickupFlow ItemPickupFlow { get; private set; }

        #endregion

        #region ========== Domain
        public LocomotionContext LocomotionContext { get; } = new();
        
        #endregion

        #region ========== Module
        public PlayerLocomotionModule LocomotionModule { get; private set; }
        #endregion

        #region ========== View
        public PlayerAnimationView AnimationView { get; private set; }

        #endregion
        public Transform PlayerTr;

        public bool CanLocomotion => _canLocomotion;
        private bool _canLocomotion;

        public bool BlockCombat => _isCombatBlocked;
        private bool _isCombatBlocked;

        public void Bind(AlphaInputSystem p_input, CameraCore p_camera,
                         DamageSystem p_damageSystem, MouseSystem p_mouseSystem)
        {
            Input = p_input;
            CameraCore = p_camera;
            DamageSystem = p_damageSystem;
            MouseSystem = p_mouseSystem;
        }

        private void Awake()
        {
            // Flow
            LocomotionModeFlow = GetComponentInChildren<LocomotionModeFlow>();
            ItemPickupFlow = GetComponent<ItemPickupFlow>();

            // Module
            LocomotionModule = GetComponentInChildren<PlayerLocomotionModule>();

            // View
            AnimationView = GetComponent<PlayerAnimationView>();

            PlayerTr = this.transform;
        }

        private void Start()
        {
            LocomotionModeFlow.Bind(this);
            LocomotionModule.Bind(LocomotionContext);

            AnimationView.Bind(PlayerTr);
        }

        public void SetCombatBlocked(bool p_isBlocked)
        {
            _isCombatBlocked = p_isBlocked;
        }

    }
}