using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public InputBoundary InputBoundary;
    public CameraModule CameraModule;
    public UIModule UIModule;
    #endregion

    #region ========== Boundary
    public AnimationBoundary AnimationBoundary;
    #endregion

    #region ========== Flow
    public StateMachine StateMachine;
    public LocomotionRule LocoRule = new LocomotionRule();
    #endregion

    #region ========== Domain
    public StateContext Context = new StateContext();
    #endregion

    #region ========== Module
    public LocomotionModule LocoModule;
    public CombatModule CombatModule;
    // Equip
    #endregion

    private void Awake()
    {
        AnimationBoundary = GetComponent<AnimationBoundary>();
        StateMachine = GetComponent<StateMachine>();
        LocoModule = GetComponent<LocomotionModule>();
        CombatModule = GetComponent<CombatModule>();

        LocoModule.Bind(this);
        CombatModule.Bind(this);
        StateMachine.Bind(this);
    }
}
