using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public InputSystem_Alpha InputManager {  get; private set; }
    public CameraCore CameraCore { get; private set; }
    public UIManager UIManager { get; private set; }

    #endregion

    #region ========== Boundary
    public AnimationBoundary AnimationBoundary;
    #endregion

    #region ========== Flow
    public PlayerStateMachine StateMachine;
    public ItemPickupController ItemPickupController;

    public LocomotionRule LocoRule = new LocomotionRule();
    public CombatRule CombatRule = new CombatRule();
    #endregion

    #region ========== Domain
    public StateContext Context = new StateContext();
    #endregion

    public Transform PlayerTr;

    #region ========== Module
    public LocomotionModule LocoModule;
    public CombatModule CombatModule;
    // Equip
    #endregion


    public bool CanLocomotion => _canLocomotion;
    private bool _canLocomotion;

    public bool BlockCombat => _canCombat;
    private bool _canCombat;

    public void Bind(InputSystem_Alpha p_input, UIManager p_ui, CameraCore p_camera)
    {
        InputManager = p_input;
        UIManager = p_ui;
        CameraCore = p_camera;
    }

    private void Awake()
    {
        AnimationBoundary = GetComponent<AnimationBoundary>();
        StateMachine = GetComponent<PlayerStateMachine>();
        LocoModule = GetComponent<LocomotionModule>();
        CombatModule = GetComponent<CombatModule>();

        PlayerTr = this.transform;
    }

    private void Start()
    {
        LocoModule.Bind(this);
        CombatModule.Bind(this);
        StateMachine.Bind(this);
        AnimationBoundary.Bind(PlayerTr);
        ItemPickupController.Bind(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            CameraCore.TransitionView(EViewType.ThirdPerson);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            CameraCore.TransitionView(EViewType.Aim);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            CameraCore.TransitionView(EViewType.Quarter);
        }
    }

    public void InventoryActiveChanged(bool p_isCombat)
    {
        _canCombat = p_isCombat;
    }

    public void SetLocomotionMode(bool p_isLocomotion)
    {
        _canLocomotion = p_isLocomotion;
    }
}
