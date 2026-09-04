using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player.Animation;
using Alpha.Player.Locomotion;
using Alpha.Player.Inventory;
using Alpha.Player.Equipment;
using Alpha.Player.Combat;
using Alpha.Player.Audio;
using Alpha.Player.Effect;
using Alpha.Player.Health;
using Alpha.Player.Actions;
using Alpha.Living;
using Alpha.Combat;
using Alpha.Rig.Player;
using UnityEngine;

namespace Alpha.Player
{
    // PlayerCore 내부 기능을 조립하고 외부 진입점을 제공한다.
    public class PlayerCore : MonoBehaviour
    {
        #region ========== OutSideBind
        public AlphaInputSystem Input { get; private set; }
        public CameraCore CameraCore { get; private set; }
        public MouseSystem MouseSystem { get; private set; }
        public ItemDatabaseManager ItemDatabase { get; private set; }
        public ResourceLoadSystem ResourceLoader { get; private set; }
        #endregion

        #region ========== Flow
        public LocomotionModeFlow LocomotionModeFlow { get; private set; }
        public InventoryFlow InventoryFlow { get; private set; }
        public CombatFlow CombatFlow { get; private set; }
        public PlayerActionFlow ActionFlow { get; private set; }
        public ItemPickupFlow ItemPickupFlow { get; private set; }
        public EquipmentFlow EquipmentFlow { get; private set; }
        #endregion

        #region ========== Domain
        public LocomotionContext LocomotionContext { get; } = new();
        public InventoryContext InventoryContext { get; } = new();
        public EquipmentContext EquipmentContext { get; } = new();

        // Combat State들이 공유하는 상태는 Player 생명주기 동안 유지한다.
        public CombatContext CombatContext { get; } = new();

        public HealthContext HealthContext { get; } = new();
        #endregion

        #region ========== Module
        public LocomotionModule LocomotionModule { get; private set; }
        public InventoryModule InventoryModule { get; private set; }
        public EquipmentModule EquipmentModule { get; private set; }
        public CombatModule CombatModule { get; private set; }
        public HealthModule HealthModule { get; private set; }
        public DamageReceiverModule DamageReceiver { get; private set; }
        #endregion

        #region ========== View
        public PlayerAnimationView AnimationView { get; private set; }
        public RigView RigView { get; private set; }
        public PlayerLocomotionAudioView LocomotionAudioView { get; private set; }
        public PlayerActionEffectView ActionEffectView { get; private set; }
        public PlayerMeleeSkillEffectView MeleeSkillEffectView { get; private set; }
        public PlayerWeaponCameraShakeView WeaponCameraShakeView { get; private set; }
        public PlayerDamageFeedbackView DamageFeedbackView { get; private set; }
        public PlayerArmorView ArmorView { get; private set; }
        public PlayerScopeView ScopeView { get; private set; }
        //public PlayerEquipmentView EquipmentView { get; private set; }

        #endregion

        public Transform PlayerTr {  get; private set; }

        // 하위 Feature는 Core를 통해 상위 ActionFlow의 허용 여부만 조회한다.
        public bool CanUseCombat =>
            ActionFlow?.AllowsCombat == true;
        public bool CanUseLocomotion =>
            ActionFlow?.AllowsLocomotion == true;

        // Installer가 소유한 입력·카메라·마우스·데이터 참조를 전달받는다.
        public void Bind(
            AlphaInputSystem p_input,
            CameraCore p_camera,
            MouseSystem p_mouseSystem,
            ItemDatabaseManager p_itemDatabase,
            ResourceLoadSystem p_resourceLoader)
        {
            Input = p_input;
            CameraCore = p_camera;
            MouseSystem = p_mouseSystem;
            ItemDatabase = p_itemDatabase;
            ResourceLoader = p_resourceLoader;
        }

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            // Flow
            LocomotionModeFlow = GetComponentInChildren<LocomotionModeFlow>(true);
            InventoryFlow = GetComponentInChildren<InventoryFlow>(true);
            EquipmentFlow = GetComponentInChildren<EquipmentFlow>(true);
            CombatFlow = GetComponentInChildren<CombatFlow>(true);
            ItemPickupFlow = GetComponentInChildren<ItemPickupFlow>();
            ActionFlow = GetComponentInChildren<PlayerActionFlow>(true);

            // Module
            LocomotionModule = GetComponentInChildren<LocomotionModule>(true);
            InventoryModule = GetComponentInChildren<InventoryModule>(true);
            EquipmentModule = GetComponentInChildren<EquipmentModule>(true);
            CombatModule = GetComponentInChildren<CombatModule>(true);
            HealthModule = GetComponentInChildren<HealthModule>(true);

            // Scene에 상위 행동 Flow가 없으면 전용 Action 하위 객체에 구성한다.
            if (ActionFlow == null)
                ActionFlow = ResolveOrCreateActionFlow();

            DamageReceiver = GetComponent<DamageReceiverModule>();

            // View
            AnimationView = GetComponentInChildren<PlayerAnimationView>(true);
            RigView = GetComponentInChildren<RigView>(true);
            LocomotionAudioView = GetComponentInChildren<PlayerLocomotionAudioView>(true);
            ActionEffectView = GetComponentInChildren<PlayerActionEffectView>(true);
            MeleeSkillEffectView = GetComponentInChildren<PlayerMeleeSkillEffectView>(true);

            // Scene에서 View가 누락되어도 기존 Effect/Combat을 표현 소유자로 사용한다.
            if (MeleeSkillEffectView == null)
            {
                Transform combatEffectOwner = transform.Find("Effect/Combat");

                if (combatEffectOwner != null)
                {
                    MeleeSkillEffectView = combatEffectOwner.gameObject
                        .AddComponent<PlayerMeleeSkillEffectView>();
                }
            }

            WeaponCameraShakeView =
                GetComponentInChildren<PlayerWeaponCameraShakeView>(true);
            DamageFeedbackView = GetComponentInChildren<PlayerDamageFeedbackView>(true);
            ArmorView = GetComponent<PlayerArmorView>();
            ScopeView = GetComponent<PlayerScopeView>();

            PlayerTr = this.transform;
        }

        private PlayerActionFlow ResolveOrCreateActionFlow()
        {
            Transform actionOwner = transform.Find("Action");

            if (actionOwner == null)
            {
                GameObject actionObject = new("Action");
                actionObject.layer = gameObject.layer;
                actionObject.transform.SetParent(transform, false);
                actionOwner = actionObject.transform;
            }

            return actionOwner.GetComponent<PlayerActionFlow>() ??
                   actionOwner.gameObject.AddComponent<PlayerActionFlow>();
        }

        // Player 내부 Feature를 Context와 실행 순서에 맞춰 초기화한다.
        private void Start()
        {
            // 이동 상태와 실제 이동 Module을 먼저 구성한다.
            LocomotionModeFlow.Bind(this);

            LocomotionModule.Bind(
                LocomotionContext,
                PlayerTr,
                DamageReceiver);

            // Inventory와 Equipment가 같은 슬롯 이동 규칙을 공유한다.
            SlotTransferModule slotTransferModule = new();

            // 인벤토리 슬롯 생성 후 입력 Flow를 연결한다.
            InventoryModule.Initialize(InventoryContext, slotTransferModule);
            InventoryFlow.Bind(InventoryContext, InventoryModule, Input);

            // 장비 상태와 인벤토리 간 이동 Flow를 연결한다.
            EquipmentModule.Bind(EquipmentContext, InventoryModule, slotTransferModule, ResourceLoader);
            EquipmentFlow.Bind(EquipmentModule, InventoryContext);
            ArmorView?.Bind(EquipmentContext, ResourceLoader);

            // 장비 변경 완료 이벤트를 실제 무기 생성 기능과 연결한다.
            if (CombatModule.Bind(this))
            {
                EquipmentFlow.OnWeaponChanged -= CombatFlow.HandleEquipmentWeaponChanged;
                EquipmentFlow.OnWeaponChanged += CombatFlow.HandleEquipmentWeaponChanged;

                CombatFlow.Bind(this);
            }

            // 픽업과 애니메이션은 앞에서 준비된 Player 기능을 사용한다.
            ItemPickupFlow.Bind(InventoryModule, ItemDatabase, Input);

            HealthModule?.Bind(HealthContext);

            if (HealthModule != null && DamageReceiver != null)
            {
                DamageReceiver.Bind(
                    PlayerTr,
                    HealthModule.TryDecreaseHealth);
                DamageReceiver.OnDamaged -= HandleDamaged;
                DamageReceiver.OnDamaged += HandleDamaged;
            }

            ActionFlow?.Bind(this);
            AnimationView?.Bind(
                ActionFlow,
                LocomotionContext,
                CombatContext);

            if (HealthModule != null)
            {
                HealthModule.OnDeath -= HandleDeath;
                HealthModule.OnDeath += HandleDeath;
            }

            RigView?.Bind(PlayerTr);

            if (RigView != null)
            {
                LocomotionContext.OnStateChanged -=
                    RigView.HandleLocomotionStateChanged;
                LocomotionContext.OnStateChanged +=
                    RigView.HandleLocomotionStateChanged;

                if (LocomotionContext.CurrentState.HasValue)
                {
                    RigView.HandleLocomotionStateChanged(
                        LocomotionContext.CurrentMode,
                        LocomotionContext.CurrentState.Value);
                }
            }

            LocomotionAudioView?.Bind(LocomotionContext);
            ActionEffectView?.Bind(LocomotionContext);
            MeleeSkillEffectView?.Bind(CombatModule);
            WeaponCameraShakeView?.Bind(CombatModule, CameraCore);
            DamageFeedbackView?.Bind(ActionFlow, CameraCore);
            ScopeView?.Bind(CameraCore);
            AnimationView.OnRootMotion -= LocomotionModule.ApplyRootMotion;
            AnimationView.OnRootMotion += LocomotionModule.ApplyRootMotion;

            if (LocomotionAudioView != null)
            {
                AnimationView.OnFootstep -= LocomotionAudioView.PlayFootstep;
                AnimationView.OnFootstep += LocomotionAudioView.PlayFootstep;
            }

        }

        // Installer의 UI 상태를 Player 상위 ActionFlow의 Combat 차단 조건으로 전달한다.
        public void SetCombatBlocked(bool p_isBlocked)
        {
            ActionFlow?.SetCombatInputBlocked(p_isBlocked);
        }

        // Core는 공용 피해 이벤트를 Player 상위 행동 Flow로 연결만 한다.
        private void HandleDamaged(Alpha.Combat.DamageInfo p_damageInfo)
        {
            ActionFlow?.HandleDamaged(p_damageInfo);
        }

        private void HandleDeath()
        {
            ActionFlow?.HandleDeath();
        }

        // Player가 연결한 장비 변경 이벤트를 해제한다.
        private void OnDestroy()
        {
            AnimationView?.Unbind();
            ActionFlow?.Unbind();

            if (DamageReceiver != null)
            {
                DamageReceiver.OnDamaged -= HandleDamaged;
                DamageReceiver.Unbind();
            }

            if (HealthModule != null)
            {
                HealthModule.OnDeath -= HandleDeath;
            }

            if (EquipmentFlow != null && CombatModule != null)
                EquipmentFlow.OnWeaponChanged -= CombatFlow.HandleEquipmentWeaponChanged;

            if (AnimationView != null && LocomotionModule != null)
                AnimationView.OnRootMotion -= LocomotionModule.ApplyRootMotion;

            if (RigView != null)
            {
                LocomotionContext.OnStateChanged -=
                    RigView.HandleLocomotionStateChanged;
            }

            if (AnimationView != null && LocomotionAudioView != null)
                AnimationView.OnFootstep -= LocomotionAudioView.PlayFootstep;

            LocomotionAudioView?.Unbind();
            ActionEffectView?.Unbind();
            MeleeSkillEffectView?.Unbind();
            WeaponCameraShakeView?.Unbind();
            DamageFeedbackView?.Unbind();
            ArmorView?.Unbind();
            EquipmentModule?.Unbind();

            ScopeView?.Unbind();
        }
    }
}
