using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public InputBoundary InputBoundary;
    public CameraCore CameraCore;
    public UIModule UIModule;
    public ItemDB ItemDB;
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
}
