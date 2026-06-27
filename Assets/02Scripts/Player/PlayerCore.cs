using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public InputManager InputManager {  get; private set; }
    public CameraCore CameraCore { get; private set; }
    public UIManager UIManager { get; private set; }
    public ItemDB ItemDB { get; private set; }
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
    public InventoryModule InventoryModule;
    // Equip
    #endregion

    public void Bind(InputManager p_input, UIManager p_ui, CameraCore p_camera, ItemDB p_item)
    {
        InputManager = p_input;
        UIManager = p_ui;
        CameraCore = p_camera;
        ItemDB = p_item;
    }

    private void Awake()
    {
        AnimationBoundary = GetComponent<AnimationBoundary>();
        StateMachine = GetComponent<PlayerStateMachine>();
        LocoModule = GetComponent<LocomotionModule>();
        CombatModule = GetComponent<CombatModule>();
        InventoryModule = GetComponent<InventoryModule>();

        PlayerTr = this.transform;
    }

    private void Start()
    {
        LocoModule.Bind(this);
        CombatModule.Bind(this);
        StateMachine.Bind(this);
        AnimationBoundary.Bind(PlayerTr);
        ItemPickupController.Bind(this);
        InventoryModule.Bind(this);
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
}
