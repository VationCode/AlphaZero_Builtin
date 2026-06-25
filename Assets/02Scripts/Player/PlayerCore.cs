using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public InputBoundary InputBoundary;
    public CameraCore CameraCore;
    public UIModule UIModule;
    #endregion

    #region ========== Boundary
    public AnimationBoundary AnimationBoundary;
    #endregion

    #region ========== Flow
    public PlayerStateMachine StateMachine;
    public LocomotionRule LocoRule = new LocomotionRule();
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
