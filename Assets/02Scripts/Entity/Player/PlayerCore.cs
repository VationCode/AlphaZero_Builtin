using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public AlphaInputSystem InputManager {  get; private set; }
    public CameraCore CameraCore { get; private set; }
    public UIManager UIManager { get; private set; }
    public InventoryPresenter InventoryPresenter { get; private set; }

    #endregion

    #region ========== Flow
    public PlayerStateMachine StateMachine { get; private set; }
    public ItemPickupFlow ItemPickupController { get; private set; }
    public PlayerInventoryFlow InventoryFlow { get; private set; }

    public LocomotionRule LocoRule = new LocomotionRule();
    public CombatRule CombatRule = new CombatRule();
    #endregion

    #region ========== Domain
    public StateContext Context = new StateContext();
    #endregion

    #region ========== Module
    public LocomotionModule LocoModule { get; private set; }
    public CombatModule CombatModule { get; private set; }
    public PlayerAnimationModule AnimationModule { get; private set; }
    public PlayerInventoryModule InventoryModule { get; private set; }
    #endregion

    public Transform PlayerTr;

    public bool CanLocomotion => _canLocomotion;
    private bool _canLocomotion;

    public bool BlockCombat => _canCombat;
    private bool _canCombat;

    public void Bind(AlphaInputSystem p_input, UIManager p_ui, CameraCore p_camera)
    {
        InputManager = p_input;
        UIManager = p_ui;
        CameraCore = p_camera;
    }

    private void Awake()
    {
        StateMachine = GetComponent<PlayerStateMachine>();
        ItemPickupController = GetComponent<ItemPickupFlow>();
        InventoryFlow = GetComponent<PlayerInventoryFlow>();

        LocoModule = GetComponent<LocomotionModule>();
        CombatModule = GetComponent<CombatModule>();
        AnimationModule = GetComponent<PlayerAnimationModule>();
        InventoryModule = GetComponent<PlayerInventoryModule>();

        PlayerTr = this.transform;
    }

    private void Start()
    {
        LocoModule.Bind(this);
        CombatModule.Bind(this);
        StateMachine.Bind(this);
        AnimationModule.Bind(PlayerTr);
        ItemPickupController.Bind(InventoryModule);
        InventoryFlow.Bind(this);
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
