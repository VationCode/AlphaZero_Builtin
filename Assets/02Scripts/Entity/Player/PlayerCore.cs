using alpha.camera;
using UnityEngine;

public class PlayerCore : MonoBehaviour
{
    #region ========== OutSideBind
    public AlphaInputSystem Input {  get; private set; }
    public CameraCore CameraCore { get; private set; }
    public UIManager UIManager { get; private set; }
    public InventoryPresenter InventoryPresenter { get; private set; }
    public ResourceLoadSystem ResourceLoader { get; private set; }

    #endregion

    #region ========== Flow
    public PlayerStateMachine StateMachineFlow { get; private set; }
    public ItemPickupFlow ItemPickupFlow { get; private set; }
    public PlayerInventoryFlow InventoryFlow { get; private set; }

    public LocomotionRule LocoRule = new LocomotionRule();
    public CombatRule CombatRule = new CombatRule();

    public PlayerEquipmentFlow EquipmentFlow { get; private set; }
    #endregion

    #region ========== Domain
    public StateContext Context = new StateContext();
    #endregion

    #region ========== Module
    public LocomotionModule LocoModule { get; private set; }
    public CombatModule CombatModule { get; private set; }
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

    public void Bind(AlphaInputSystem p_input, UIManager p_ui, CameraCore p_camera, ResourceLoadSystem p_resourceLoad)
    {
        Input = p_input;
        UIManager = p_ui;
        CameraCore = p_camera;
        ResourceLoader = p_resourceLoad;
    }   

    private void Awake()
    {
        StateMachineFlow = GetComponent<PlayerStateMachine>();
        ItemPickupFlow = GetComponent<ItemPickupFlow>();
        InventoryFlow = GetComponent<PlayerInventoryFlow>();
        EquipmentFlow = GetComponent<PlayerEquipmentFlow>();

        LocoModule = GetComponent<LocomotionModule>();
        CombatModule = GetComponent<CombatModule>();
        AnimationView = GetComponent<PlayerAnimationView>();
        InventoryModule = GetComponent<PlayerInventoryModule>();
        EquipmentModule = GetComponent<PlayerEquipmentModule>();

        EquipmentView = GetComponent<PlayerEquipmentView>();

        PlayerTr = this.transform;
    }

    private void Start()
    {
        StateMachineFlow.Bind(this);
        InventoryFlow.Bind(this);
        ItemPickupFlow.Bind(InventoryModule);
        if (EquipmentFlow == null || EquipmentView == null)
        {
            Debug.LogError("[PlayerCore] PlayerEquipmentFlow 또는 PlayerEquipmentView가 없습니다.",this);

            return;
        }
        EquipmentFlow.Bind(EquipmentModule, EquipmentView, ResourceLoader);
        
        LocoModule.Bind(this);
        CombatModule.Bind(this);
        
        AnimationView.Bind(PlayerTr);

    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha8))
        {
            CameraCore.TransitionView(EViewType.ThirdPerson);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha9))
        {
            CameraCore.TransitionView(EViewType.Aim);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha0))
        {
            CameraCore.TransitionView(EViewType.Quarter);
        }
    }

    public void SetCombatBlocked(bool p_isBlocked)
    {
        _isCombatBlocked = p_isBlocked;
    }

}
