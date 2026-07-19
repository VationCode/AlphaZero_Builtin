using System;
using Unity.VisualScripting;
using UnityEngine;

// 추적, 회전, 줌, 충돌, 부드러운 보간 실행
namespace Alpha.AlphaCamera
{
    public class CameraRigModule : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _cameraShoulder;
        [SerializeField] private Transform _cameraZoomHolder;
        [SerializeField] private UnityEngine.Camera _renderCamera;

        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private float _collisionPadding = 0.1f;
        [SerializeField] private float _collisionMinDistance = 0.2f;

        private Transform _followTarget;

        private CameraPose _transitionStartPose;
        private CameraPose _transitionTargetPose;
        private AnimationCurve _transitionCurve;

        private float _transitionDuration;
        private float _transitionTime;

        public UnityEngine.Camera RenderCamera => _renderCamera;

        public bool IsTransitioning { get; private set; }

        public void BindTarget(Transform p_followTarget)
        {
            _followTarget = p_followTarget;
        }

        #region ==================== Follow
        // Rig를 추적 대상 위치로 즉시 이동한다.
        public void SnapToTarget()
        {
            transform.position = _followTarget.position;
        }

        // Rig가 추적 대상을 부드럽게 따라간다.
        public void FollowTarget(float p_followSpeed, float p_deltaTime)
        {
            float followTime = 1f - Mathf.Exp(-p_followSpeed * p_deltaTime);

            transform.position = Vector3.Lerp(transform.position, _followTarget.position, followTime);
        }
        #endregion ==================== /Follow
        #region ==================== Control
        public void Rotate(CameraContext p_context, Vector2 p_lookInput, CameraViewSO p_profile, float p_deltaTime)
        {
            if (p_context == null || p_profile == null)
                return;

            p_context.Pitch -= p_lookInput.y * p_profile.LookSensitivity * p_deltaTime;

            p_context.Yaw += p_lookInput.x * p_profile.LookSensitivity * p_deltaTime;

            p_context.Pitch = Mathf.Clamp(p_context.Pitch, p_profile.MinPitch, p_profile.MaxPitch);
        }

        public void Zoom(CameraContext p_context, float p_scrollInput, CameraViewSO p_profile)
        {
            if (p_context == null || p_profile == null)
                return;

            p_context.ZoomDistance -= p_scrollInput * p_profile.ZoomScrollSpeed;

            p_context.ZoomDistance = Mathf.Clamp(p_context.ZoomDistance, p_profile.ZoomMinDistance, p_profile.ZoomMaxDistance);
        }
        #endregion ==================== /Control

        #region ==================== Pose
        // ApplyPose → Pivot, Shoulder, Zoom, FOV 반영
        public void ApplyPose(CameraPose p_pose)
        {
            _cameraPivot.localPosition = p_pose.PivotPosition;

            _cameraPivot.localRotation = p_pose.PivotRotation;

            _cameraShoulder.localPosition = p_pose.ShoulderPosition;

            _renderCamera.fieldOfView = p_pose.FieldOfView;

            _cameraZoomHolder.localPosition = ResolveCollision(p_pose.ZoomPosition);
        }

        // ResolveCollision → Shoulder와 Camera 사이 장애물 처리
        private Vector3 ResolveCollision(Vector3 p_desiredPosition)
        {
            Vector3 desiredWorldPosition = _cameraShoulder.TransformPoint(p_desiredPosition);

            bool hasCollision = Physics.Linecast(_cameraShoulder.position, desiredWorldPosition, out RaycastHit hit,_collisionMask, QueryTriggerInteraction.Ignore);

            if (!hasCollision)
            {
                return p_desiredPosition;
            }

            float desiredDistance = Mathf.Abs(p_desiredPosition.z);

            float collisionDistance = 
                Mathf.Clamp(hit.distance - _collisionPadding, _collisionMinDistance, desiredDistance);

            return Vector3.back * collisionDistance;
        }
        private CameraPose GetCurrentPose()
        {
            return new CameraPose(_cameraPivot.localPosition, _cameraPivot.localRotation,
                                  _cameraShoulder.localPosition, _cameraZoomHolder.localPosition,
                                  _renderCamera.fieldOfView);
        }
        #endregion ==================== /Pose

        #region ==================== Transition
        // 현재 Pose에서 목표 Pose로 전환을 시작한다.
        public void BeginIsTransition(CameraPose p_targetPose, float p_duration, AnimationCurve p_curve)
        {
            if (p_duration <= 0f)
            {
                ApplyPose(p_targetPose);
                IsTransitioning = false;
                return;
            }

            _transitionStartPose = GetCurrentPose();
            _transitionTargetPose = p_targetPose;
            _transitionDuration = p_duration;
            _transitionCurve = p_curve;
            _transitionTime = 0f;

            IsTransitioning = true;
        }

        // 진행 시간에 맞춰 전환 Pose를 적용한다.
        public void UpdateTransition(float p_deltaTime)
        {
            if (!IsTransitioning)
                return;

            _transitionTime += p_deltaTime;

            float normalizedTime = Mathf.Clamp01(_transitionTime / _transitionDuration);

            if (normalizedTime >= 1f)
            {
                ApplyPose(_transitionTargetPose);
                IsTransitioning = false;
                return;
            }

            float transitionTime = _transitionCurve.Evaluate(normalizedTime);

            CameraPose currentPose = CameraPose.Lerp(_transitionStartPose, _transitionTargetPose, transitionTime);

            ApplyPose(currentPose);
        }
        #endregion ==================== /Transition

        #region ==================== Aim
        // 화면 중앙을 향하는 Ray를 생성한다.
        public Ray GetCenterRay()
        {
            return _renderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        }

        #endregion ==================== /Aim

        private void OnValidate()
        {
            _collisionPadding = Mathf.Max(0f, _collisionPadding);

            _collisionMinDistance = Mathf.Max(0f, _collisionMinDistance);
        }
    }
}
