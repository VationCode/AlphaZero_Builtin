using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Rig.Player
{
    public enum EHandIKPolicy
    {
        Inherit = 0,
        Enable = 1,
        Disable = 2
    }

    [System.Serializable]
    public sealed class HandIKAnimationRule
    {
        [SerializeField]
        [Tooltip("Animator State의 전체 경로입니다. 예: Base Layer.Fast Run F")]
        private string _stateFullPath;

        [SerializeField]
        private EHandIKPolicy _leftHand = EHandIKPolicy.Inherit;

        [SerializeField]
        private EHandIKPolicy _rightHand = EHandIKPolicy.Inherit;

        public string StateFullPath => _stateFullPath;
        public EHandIKPolicy LeftHand => _leftHand;
        public EHandIKPolicy RightHand => _rightHand;

        public HandIKAnimationRule()
        {
        }

        public HandIKAnimationRule(
            string p_stateFullPath,
            EHandIKPolicy p_leftHand,
            EHandIKPolicy p_rightHand)
        {
            _stateFullPath = p_stateFullPath;
            _leftHand = p_leftHand;
            _rightHand = p_rightHand;
        }
    }

    // Player의 조준 상체 자세와 양손 IK를 Animator Rig 표현으로 적용한다.
    [RequireComponent(typeof(Animator))]
    public class RigView : MonoBehaviour
    {
        private const string WeaponUpperBodyLayerName = "Weapon UpperBody Layer";

        [Header("Aim Rig")]
        [SerializeField]
        [Tooltip("조준 및 Range 공격 중 상체 Aiming 자세와 Pitch 보정을 적용합니다.")]
        private bool _enableAimRig = true;

        [Header("Aim Pitch")]
        [SerializeField, Min(0f)]
        private float _maxAimUpAngle = 45f;

        [SerializeField, Min(0f)]
        private float _maxAimDownAngle = 35f;

        [SerializeField, Min(0f)]
        private float _aimPitchSmoothTime = 0.02f;

        [SerializeField, Range(0f, 1f)]
        private float _spinePitchWeight = 0.2f;

        [SerializeField, Range(0f, 1f)]
        private float _chestPitchWeight = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float _upperChestPitchWeight = 0.4f;

        [Header("Range Pose Locomotion")]
        [SerializeField]
        [Tooltip("Ground 이외의 이동 Mode에서는 Range 상체 자세와 손 IK를 해제합니다.")]
        private bool _suppressRangeRigOutsideGround = true;

        [SerializeField]
        [Tooltip("Range Rig를 해제할 Locomotion State입니다. 배열에서 제거하면 해당 상태에도 적용됩니다.")]
        private ELocoStateType[] _suppressedLocomotionStates =
        {
            ELocoStateType.Jump,
            ELocoStateType.Fall,
            ELocoStateType.Land,
            ELocoStateType.Dash,
            ELocoStateType.Dodge,
            ELocoStateType.Die,
            ELocoStateType.Rising
        };

        [Header("Left Hand IK")]
        [SerializeField]
        private bool _enableLeftHandIK = true;

        [SerializeField, Range(0f, 1f)]
        private float _leftHandPositionWeight = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _leftHandRotationWeight = 1f;

        [SerializeField, Min(0f)]
        private float _handIKBlendSpeed = 10f;

        [Header("Right Hand IK")]
        [SerializeField]
        private bool _enableRightHandIK = true;

        [SerializeField, Range(0f, 1f)]
        private float _rightHandPositionWeight = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _rightHandRotationWeight = 1f;

        [Header("Hand IK Animation Rules")]
        [SerializeField]
        [Tooltip("현재 또는 전환 중인 Animator State에 따라 손별 IK를 제어합니다. 아래쪽 규칙이 우선합니다.")]
        private HandIKAnimationRule[] _handIKAnimationRules =
        {
            new(
                "Base Layer.Fast Run F",
                EHandIKPolicy.Disable,
                EHandIKPolicy.Inherit)
        };

        private Animator _anim;
        private Transform _ownerTr;
        private Transform _spineBone;
        private Transform _chestBone;
        private Transform _upperChestBone;

        private int _weaponUpperBodyLayerIndex = -1;
        private bool _hasRangeWeaponRig;
        private bool _holdRangePoseWhileEquipped;
        private bool _isRangeCombatPoseRequested;
        private bool _isRangePoseActive;
        private Vector3 _aimDirection;
        private float _currentAimPitch;
        private float _aimPitchVelocity;

        private Transform _leftHandIKTarget;
        private Vector3 _leftHandIKPosition;
        private Quaternion _leftHandIKRotation = Quaternion.identity;
        private float _currentLeftHandIKWeight;
        private float _weaponLeftHandPositionWeight = 1f;
        private float _weaponLeftHandRotationWeight = 1f;

        private Transform _rightHandIKTarget;
        private Vector3 _rightHandIKPosition;
        private Quaternion _rightHandIKRotation = Quaternion.identity;
        private float _currentRightHandIKWeight;

        private bool _isRangeRigSwapSuppressed;
        private bool _isRangeRigLocomotionSuppressed;

        private bool IsRangeRigSuppressed =>
            _isRangeRigSwapSuppressed ||
            _isRangeRigLocomotionSuppressed;

        private bool IsRangeRigRequested =>
            _hasRangeWeaponRig &&
            (_holdRangePoseWhileEquipped ||
             _isRangeCombatPoseRequested);

        private static readonly int WeaponUpperBodyNoneState =
            Animator.StringToHash("Weapon UpperBody Layer.None");

        private static readonly int AimingState =
            Animator.StringToHash("Weapon UpperBody Layer.Aiming");

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            CacheAimBones();
            RefreshAnimatorLayer();
        }

        private void Update()
        {
            UpdateHandIKBlend();
        }

        private void LateUpdate()
        {
            SynchronizeRangePoseState();
            UpdateAimPitch();
        }

        // Weapon UpperBody Layer 평가 후 활성화된 손을 각 IK Target에 고정한다.
        private void OnAnimatorIK(int p_layerIndex)
        {
            if (_anim == null ||
                p_layerIndex != _weaponUpperBodyLayerIndex)
            {
                return;
            }

            CacheHandIKPoses();

            ApplyHandIK(
                AvatarIKGoal.LeftHand,
                _currentLeftHandIKWeight,
                _leftHandPositionWeight *
                _weaponLeftHandPositionWeight,
                _leftHandRotationWeight *
                _weaponLeftHandRotationWeight,
                _leftHandIKPosition,
                _leftHandIKRotation);

            ApplyHandIK(
                AvatarIKGoal.RightHand,
                _currentRightHandIKWeight,
                _rightHandPositionWeight,
                _rightHandRotationWeight,
                _rightHandIKPosition,
                _rightHandIKRotation);
        }

        // 조준 방향의 로컬 기준과 상체 회전축으로 사용할 Entity Transform을 연결한다.
        public void Bind(Transform p_ownerTr)
        {
            _ownerTr = p_ownerTr;
        }

        // Animator Controller 교체 후 Rig가 사용할 Layer Index와 Weight를 복구한다.
        public void RefreshAnimatorLayer()
        {
            if (_anim == null)
                return;

            _weaponUpperBodyLayerIndex =
                _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            _isRangePoseActive = false;

            if (_weaponUpperBodyLayerIndex < 0)
            {
                Debug.LogError(
                    $"Animator Layer를 찾을 수 없습니다: {WeaponUpperBodyLayerName}",
                    this);
                return;
            }

            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
        }

        // Range 전투 Flow가 판단한 조준·공격 자세 요청을 반영한다.
        public void SetRangeCombatPose(bool p_isRequested)
        {
            _isRangeCombatPoseRequested = p_isRequested;
            ApplyRangePoseState(0.1f);
        }

        // 실제 Range 공격과 동일한 월드 조준 방향을 상체 Pitch 입력으로 보관한다.
        public void SetAimDirection(Vector3 p_worldDirection)
        {
            _aimDirection = p_worldDirection.sqrMagnitude > 0.0001f
                ? p_worldDirection.normalized
                : Vector3.zero;
        }

        public void ClearAimDirection()
        {
            _aimDirection = Vector3.zero;
        }

        // Range 무기의 표현 설정을 값으로 복사해 Item View에 대한 지속 참조를 피한다.
        public void BindRangeWeaponRig(
            Transform p_leftHandTarget,
            float p_positionWeight,
            float p_rotationWeight,
            bool p_holdPoseWhileEquipped)
        {
            _hasRangeWeaponRig = true;
            _holdRangePoseWhileEquipped = p_holdPoseWhileEquipped;
            _weaponLeftHandPositionWeight =
                Mathf.Clamp01(p_positionWeight);
            _weaponLeftHandRotationWeight =
                Mathf.Clamp01(p_rotationWeight);

            SetLeftHandIKTarget(p_leftHandTarget);
            ApplyRangePoseState(0.1f);
        }

        // Range 장착이 해제되면 상체 자세와 손 Target을 함께 정리한다.
        public void ClearRangeWeaponRig()
        {
            _hasRangeWeaponRig = false;
            _holdRangePoseWhileEquipped = false;
            _isRangeCombatPoseRequested = false;
            _weaponLeftHandPositionWeight = 1f;
            _weaponLeftHandRotationWeight = 1f;
            ClearAimDirection();
            ClearLeftHandIKTarget();
            ApplyRangePoseState(0.1f);
        }

        // 현재 무기의 왼손 지지점을 IK Target으로 교체한다.
        public void SetLeftHandIKTarget(Transform p_target)
        {
            _leftHandIKTarget = p_target;
            CacheLeftHandIKPose();
        }

        public void ClearLeftHandIKTarget()
        {
            CacheLeftHandIKPose();
            _leftHandIKTarget = null;
        }

        // 오른손 IK가 필요한 View가 외부 기준 Target을 연결할 수 있는 진입점이다.
        public void SetRightHandIKTarget(Transform p_target)
        {
            _rightHandIKTarget = p_target;
            CacheRightHandIKPose();
        }

        public void ClearRightHandIKTarget()
        {
            CacheRightHandIKPose();
            _rightHandIKTarget = null;
        }

        // Swap처럼 Animation Clip이 손을 직접 제어하는 동안 IK 적용을 잠시 차단한다.
        public void SetHandIKSuppressed(
            bool p_isSuppressed,
            bool p_isImmediate = false)
        {
            _isRangeRigSwapSuppressed = p_isSuppressed;

            ApplyRangePoseState(
                p_isImmediate ? 0f : 0.05f);

            if (!p_isImmediate)
                return;

            _currentLeftHandIKWeight =
                ResolveTargetHandIKWeight(AvatarIKGoal.LeftHand);

            _currentRightHandIKWeight =
                ResolveTargetHandIKWeight(AvatarIKGoal.RightHand);
        }

        // 무기별 상체 애니메이션을 지원하지 않는 이동 상태에서는 양손 IK를 해제한다.
        public void HandleLocomotionStateChanged(
            ELocomotionMode p_mode,
            ELocoStateType p_state)
        {
            _isRangeRigLocomotionSuppressed =
                ShouldSuppressRangeRig(p_mode, p_state);

            ApplyRangePoseState(0.05f);
        }

        private bool ShouldSuppressRangeRig(
            ELocomotionMode p_mode,
            ELocoStateType p_state)
        {
            if (_suppressRangeRigOutsideGround &&
                p_mode != ELocomotionMode.Ground)
            {
                return true;
            }

            if (_suppressedLocomotionStates == null)
                return false;

            foreach (ELocoStateType suppressedState in
                     _suppressedLocomotionStates)
            {
                if (suppressedState == p_state)
                    return true;
            }

            return false;
        }

        private void CacheAimBones()
        {
            if (_anim == null || !_anim.isHuman)
                return;

            _spineBone = _anim.GetBoneTransform(HumanBodyBones.Spine);
            _chestBone = _anim.GetBoneTransform(HumanBodyBones.Chest);
            _upperChestBone = _anim.GetBoneTransform(HumanBodyBones.UpperChest);
        }

        private void ApplyRangePoseState(float p_transitionDuration)
        {
            if (_anim == null || _weaponUpperBodyLayerIndex < 0)
                return;

            bool shouldActivate =
                _enableAimRig &&
                IsRangeRigRequested &&
                !IsRangeRigSuppressed;

            int targetState = shouldActivate
                ? AimingState
                : WeaponUpperBodyNoneState;

            if (!_anim.HasState(_weaponUpperBodyLayerIndex, targetState))
            {
                Debug.LogError(
                    $"Weapon UpperBody 상태를 찾을 수 없습니다: " +
                    $"{(shouldActivate ? "Aiming" : "None")}",
                    this);
                return;
            }

            if (_isRangePoseActive == shouldActivate &&
                IsWeaponUpperBodyStateActive(targetState))
            {
                return;
            }

            _isRangePoseActive = shouldActivate;
            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
            _anim.CrossFadeInFixedTime(
                targetState,
                p_transitionDuration,
                _weaponUpperBodyLayerIndex,
                0f);
        }

        private void SynchronizeRangePoseState()
        {
            bool shouldActivate =
                _enableAimRig &&
                IsRangeRigRequested &&
                !IsRangeRigSuppressed;

            // 비활성 상태에서는 Swap 같은 다른 상체 애니메이션을 None으로 덮지 않는다.
            if (!shouldActivate && !_isRangePoseActive)
                return;

            ApplyRangePoseState(0.05f);
        }

        private void UpdateAimPitch()
        {
            if (_ownerTr == null ||
                !_enableAimRig ||
                !_isRangeCombatPoseRequested ||
                IsRangeRigSuppressed)
            {
                _currentAimPitch = 0f;
                _aimPitchVelocity = 0f;
                return;
            }

            _currentAimPitch = Mathf.SmoothDampAngle(
                _currentAimPitch,
                ResolveAimPitch(),
                ref _aimPitchVelocity,
                _aimPitchSmoothTime);

            if (Mathf.Abs(_currentAimPitch) <= 0.001f)
                return;

            ApplyBonePitch(_spineBone, _spinePitchWeight);
            ApplyBonePitch(_chestBone, _chestPitchWeight);
            ApplyBonePitch(_upperChestBone, _upperChestPitchWeight);
        }

        private float ResolveAimPitch()
        {
            if (_aimDirection.sqrMagnitude <= 0.0001f)
                return 0f;

            Vector3 localDirection =
                _ownerTr.InverseTransformDirection(_aimDirection);

            float horizontalLength = new Vector2(
                localDirection.x,
                localDirection.z).magnitude;

            float pitch = Mathf.Atan2(
                localDirection.y,
                horizontalLength) * Mathf.Rad2Deg;

            return Mathf.Clamp(
                pitch,
                -_maxAimDownAngle,
                _maxAimUpAngle);
        }

        private void ApplyBonePitch(Transform p_bone, float p_weight)
        {
            if (p_bone == null || p_weight <= 0f)
                return;

            p_bone.rotation = Quaternion.AngleAxis(
                -_currentAimPitch * p_weight,
                _ownerTr.right) * p_bone.rotation;
        }

        private void UpdateHandIKBlend()
        {
            float leftTargetWeight =
                ResolveTargetHandIKWeight(AvatarIKGoal.LeftHand);

            float rightTargetWeight =
                ResolveTargetHandIKWeight(AvatarIKGoal.RightHand);

            _currentLeftHandIKWeight = BlendHandIKWeight(
                _currentLeftHandIKWeight,
                leftTargetWeight);

            _currentRightHandIKWeight = BlendHandIKWeight(
                _currentRightHandIKWeight,
                rightTargetWeight);

            CacheHandIKPoses();
        }

        private float ResolveTargetHandIKWeight(AvatarIKGoal p_goal)
        {
            Transform target = p_goal == AvatarIKGoal.LeftHand
                ? _leftHandIKTarget
                : _rightHandIKTarget;

            bool defaultEnabled = p_goal == AvatarIKGoal.LeftHand
                ? _enableLeftHandIK
                : _enableRightHandIK;

            return IsRangeRigRequested &&
                   target != null &&
                   !IsRangeRigSuppressed &&
                   ResolveAnimationHandIKEnabled(p_goal, defaultEnabled)
                ? 1f
                : 0f;
        }

        // 현재와 전환 대상 State의 규칙을 순서대로 적용한다.
        private bool ResolveAnimationHandIKEnabled(
            AvatarIKGoal p_goal,
            bool p_defaultEnabled)
        {
            bool isEnabled = p_defaultEnabled;

            if (_anim == null || _handIKAnimationRules == null)
                return isEnabled;

            foreach (HandIKAnimationRule rule in _handIKAnimationRules)
            {
                if (rule == null ||
                    string.IsNullOrWhiteSpace(rule.StateFullPath))
                {
                    continue;
                }

                int stateHash = Animator.StringToHash(rule.StateFullPath);

                if (!IsAnimatorStateActive(stateHash))
                    continue;

                EHandIKPolicy policy = p_goal == AvatarIKGoal.LeftHand
                    ? rule.LeftHand
                    : rule.RightHand;

                if (policy == EHandIKPolicy.Enable)
                    isEnabled = true;
                else if (policy == EHandIKPolicy.Disable)
                    isEnabled = false;
            }

            return isEnabled;
        }

        private bool IsAnimatorStateActive(int p_stateHash)
        {
            for (int layerIndex = 0;
                 layerIndex < _anim.layerCount;
                 layerIndex++)
            {
                AnimatorStateInfo currentState =
                    _anim.GetCurrentAnimatorStateInfo(layerIndex);

                if (currentState.fullPathHash == p_stateHash)
                    return true;

                if (!_anim.IsInTransition(layerIndex))
                    continue;

                AnimatorStateInfo nextState =
                    _anim.GetNextAnimatorStateInfo(layerIndex);

                if (nextState.fullPathHash == p_stateHash)
                    return true;
            }

            return false;
        }

        private float BlendHandIKWeight(
            float p_currentWeight,
            float p_targetWeight)
        {
            return _handIKBlendSpeed > 0f
                ? Mathf.MoveTowards(
                    p_currentWeight,
                    p_targetWeight,
                    _handIKBlendSpeed * Time.deltaTime)
                : p_targetWeight;
        }

        private void ApplyHandIK(
            AvatarIKGoal p_goal,
            float p_currentWeight,
            float p_positionWeight,
            float p_rotationWeight,
            Vector3 p_position,
            Quaternion p_rotation)
        {
            _anim.SetIKPositionWeight(
                p_goal,
                p_currentWeight * p_positionWeight);

            _anim.SetIKRotationWeight(
                p_goal,
                p_currentWeight * p_rotationWeight);

            if (p_currentWeight <= 0f)
                return;

            _anim.SetIKPosition(p_goal, p_position);
            _anim.SetIKRotation(p_goal, p_rotation);
        }

        private void CacheHandIKPoses()
        {
            CacheLeftHandIKPose();
            CacheRightHandIKPose();
        }

        private void CacheLeftHandIKPose()
        {
            if (_leftHandIKTarget == null)
                return;

            _leftHandIKPosition = _leftHandIKTarget.position;
            _leftHandIKRotation = _leftHandIKTarget.rotation;
        }

        private void CacheRightHandIKPose()
        {
            if (_rightHandIKTarget == null)
                return;

            _rightHandIKPosition = _rightHandIKTarget.position;
            _rightHandIKRotation = _rightHandIKTarget.rotation;
        }

        private bool IsWeaponUpperBodyStateActive(int p_stateHash)
        {
            AnimatorStateInfo currentState =
                _anim.GetCurrentAnimatorStateInfo(_weaponUpperBodyLayerIndex);

            if (currentState.fullPathHash == p_stateHash)
                return true;

            if (!_anim.IsInTransition(_weaponUpperBodyLayerIndex))
                return false;

            AnimatorStateInfo nextState =
                _anim.GetNextAnimatorStateInfo(_weaponUpperBodyLayerIndex);

            return nextState.fullPathHash == p_stateHash;
        }
    }
}
