using UnityEngine;

namespace Alpha.AlphaCamera
{
    // Camera Rig의 추적과 최종 Pose 적용을 담당한다.
    public class CameraRigModule : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _cameraShoulder;
        [SerializeField] private Transform _cameraPositionRoot;
        [SerializeField] private UnityEngine.Camera _renderCamera;

        private Transform _followTarget;

        public UnityEngine.Camera RenderCamera => _renderCamera;
        // 장애물 검사의 시작점으로 CameraShoulder를 제공한다.
        public Transform ObstructionOrigin => _cameraShoulder;

        public bool IsBound { get; private set; }

        public bool Bind(Transform p_followTarget)
        {
            if (p_followTarget == null || _cameraPivot == null ||
                _cameraShoulder == null || _cameraPositionRoot == null ||
                _renderCamera == null)
            {
                Debug.LogError($"{nameof(CameraRigModule)}의 참조가 설정되지 않았습니다.", this);

                return false;
            }

            _followTarget = p_followTarget;
            IsBound = true;

            return TrySnapToTarget();
        }

        public bool TrySnapToTarget()
        {
            if (!IsBound)
                return false;

            transform.position = _followTarget.position;
            return true;
        }

        public bool TryFollowTarget(float p_followSpeed, float p_deltaTime)
        {
            if (!IsBound)
                return false;

            float followRatio = 1f - Mathf.Exp(-Mathf.Max(0f, p_followSpeed) * Mathf.Max(0f, p_deltaTime));

            transform.position = Vector3.Lerp(transform.position, _followTarget.position, followRatio);

            return true;
        }

        public bool TryApplyPose(CameraPose p_pose)
        {
            if (!IsBound)
                return false;

            _cameraPivot.localPosition = p_pose.PivotLocalPosition;

            // 월드 회전으로 적용
            _cameraPivot.rotation = p_pose.PivotWorldRotation;

            _cameraShoulder.localPosition = p_pose.ShoulderLocalPosition;

            _cameraPositionRoot.localPosition = p_pose.CameraLocalPosition;

            _renderCamera.fieldOfView = Mathf.Clamp(p_pose.FieldOfView, 1f, 179f);

            return true;
        }

        public bool TryApplyCameraDistance(float p_resolvedDistance)
        {
            if (!IsBound)
                return false;

            Vector3 localPosition = _cameraPositionRoot.localPosition;

            // 기존 X/Y 구도는 유지하고 거리만 보정한다.
            localPosition.z = -Mathf.Max(0f, p_resolvedDistance);

            _cameraPositionRoot.localPosition = localPosition;

            return true;
        }

        // 장애물 보정까지 반영된 현재 실제 카메라 구도를 반환한다.
        public bool TryGetCurrentPose(out CameraPose p_pose)
        {
            p_pose = default;

            if (!IsBound)
                return false;

            p_pose = new CameraPose(
                                    _cameraPivot.localPosition, _cameraPivot.rotation,
                                    _cameraShoulder.localPosition, _cameraPositionRoot.localPosition,
                                    _renderCamera.fieldOfView);

            return true;
        }
    }
}
