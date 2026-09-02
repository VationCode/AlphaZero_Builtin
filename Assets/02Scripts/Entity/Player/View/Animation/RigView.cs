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
        private bool _isAimRigRequested;
        private bool _isAiming;
        private Vector3 _aimDirection;
        private float _currentAimPitch;
        private float _aimPitchVelocity;

        private Transform _leftHandIKTarget;
        private Vector3 _leftHandIKPosition;
        private Quaternion _leftHandIKRotation = Quaternion.identity;
        private float _currentLeftHandIKWeight;

        private Transform _rightHandIKTarget;
        private Vector3 _rightHandIKPosition;
        private Quaternion _rightHandIKRotation = Quaternion.identity;
        private float _currentRightHandIKWeight;

        private bool _isHandIKSwapSuppressed;
        private bool _isHandIKLocomotionSuppressed;

        private bool IsHandIKSuppressed =>
            _isHandIKSwapSuppressed ||
            _isHandIKLocomotionSuppressed;

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
            SynchronizeAimingState();
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
                _leftHandPositionWeight,
                _leftHandRotationWeight,
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

            _isAiming = false;

            if (_weaponUpperBodyLayerIndex < 0)
            {
                Debug.LogError(
                    $"Animator Layer를 찾을 수 없습니다: {WeaponUpperBodyLayerName}",
                    this);
                return;
            }

            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
        }

        // Flow가 판단한 조준 자세 요청을 Inspector 설정과 함께 적용한다.
        public void SetAiming(bool p_isRequested)
        {
            _isAimRigRequested = p_isRequested;
            ApplyAimingState(0.1f);
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
            _isHandIKSwapSuppressed = p_isSuppressed;

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
            bool isUnsupportedGroundState =
                p_state == ELocoStateType.Jump ||
                p_state == ELocoStateType.Fall ||
                p_state == ELocoStateType.Land ||
                p_state == ELocoStateType.Dash ||
                p_state == ELocoStateType.Die;

            _isHandIKLocomotionSuppressed =
                p_mode != ELocomotionMode.Ground ||
                isUnsupportedGroundState;
        }

        private void CacheAimBones()
        {
            if (_anim == null || !_anim.isHuman)
                return;

            _spineBone = _anim.GetBoneTransform(HumanBodyBones.Spine);
            _chestBone = _anim.GetBoneTransform(HumanBodyBones.Chest);
            _upperChestBone = _anim.GetBoneTransform(HumanBodyBones.UpperChest);
        }

        private void ApplyAimingState(float p_transitionDuration)
        {
            if (_anim == null || _weaponUpperBodyLayerIndex < 0)
                return;

            bool shouldActivate =
                _enableAimRig &&
                _isAimRigRequested;

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

            if (_isAiming == shouldActivate &&
                IsWeaponUpperBodyStateActive(targetState))
            {
                return;
            }

            _isAiming = shouldActivate;
            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
            _anim.CrossFadeInFixedTime(
                targetState,
                p_transitionDuration,
                _weaponUpperBodyLayerIndex,
                0f);
        }

        private void SynchronizeAimingState()
        {
            bool shouldActivate =
                _enableAimRig &&
                _isAimRigRequested;

            // 비활성 상태에서는 Swap 같은 다른 상체 애니메이션을 None으로 덮지 않는다.
            if (!shouldActivate && !_isAiming)
                return;

            ApplyAimingState(0.05f);
        }

        private void UpdateAimPitch()
        {
            if (_ownerTr == null || !_enableAimRig)
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

            return target != null &&
                   !IsHandIKSuppressed &&
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
