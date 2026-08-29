using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 대표 공격 Module이 확정한 Hitscan 경로를 Tracer와 충돌 Effect로 표현한다.
    public sealed class HitscanEffectView : MonoBehaviour
    {
        [SerializeField]
        private RangeAttackModule _attackModule;

        [SerializeField]
        private BulletTracerView _tracerPrefab;

        [SerializeField]
        private ParticleSystem _impactPrefab;

        [SerializeField, Min(0.01f)]
        private float _impactLifetime = 5f;

        [Header("Scope Tracer")]
        [Tooltip(
            "Scope Tracer 시작 Viewport 위치입니다. " +
            "(0.5, 0.5)는 화면 중앙이며 Y가 작을수록 아래입니다.")]
        [SerializeField]
        private Vector2 _scopeTracerViewportPosition =
            new(0.5f, 0.25f);

        [SerializeField, Min(0f)]
        private float _scopeTracerDepthOffset = 0.05f;

        private CameraCore _cameraCore;

        private void Awake()
        {
            _attackModule ??= GetComponent<RangeAttackModule>();
        }

        // Player가 장착한 무기에서만 Scope용 화면 시작점을 계산한다.
        public void BindCamera(CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void OnEnable()
        {
            if (_attackModule != null)
            {
                _attackModule.OnTrajectoryResolved +=
                    HandleTrajectoryResolved;
            }
        }

        private void OnDisable()
        {
            if (_attackModule != null)
            {
                _attackModule.OnTrajectoryResolved -=
                    HandleTrajectoryResolved;
            }
        }

        private void HandleTrajectoryResolved(
            RangeAttackResult p_result)
        {
            if (_tracerPrefab != null)
            {
                Vector3 tracerStart =
                    ResolveTracerStart(p_result.StartPoint);

                BulletTracerView tracer = Instantiate(
                    _tracerPrefab,
                    tracerStart,
                    Quaternion.identity);

                tracer.Play(
                    tracerStart,
                    p_result.EndPoint);
            }

            if (!p_result.HasCollision ||
                _impactPrefab == null)
            {
                return;
            }

            ParticleSystem impact = Instantiate(
                _impactPrefab,
                p_result.EndPoint,
                Quaternion.LookRotation(
                    p_result.CollisionNormal));

            impact.Play(true);
            Destroy(
                impact.gameObject,
                _impactLifetime);
        }

        // Scope에서는 판정 경로와 분리해 화면 중앙 아래에서 Tracer만 시작한다.
        private Vector3 ResolveTracerStart(
            Vector3 p_defaultStart)
        {
            if (_cameraCore?.RenderCamera == null ||
                _cameraCore.Context.EffectiveViewType !=
                ECameraViewType.Scope)
            {
                return p_defaultStart;
            }

            Camera renderCamera =
                _cameraCore.RenderCamera;

            Ray scopeRay = renderCamera.ViewportPointToRay(
                new Vector3(
                    _scopeTracerViewportPosition.x,
                    _scopeTracerViewportPosition.y));

            float startDistance = Mathf.Max(
                renderCamera.nearClipPlane +
                _scopeTracerDepthOffset,
                0.01f);

            return scopeRay.GetPoint(startDistance);
        }

        private void OnValidate()
        {
            _impactLifetime = Mathf.Max(
                0.01f,
                _impactLifetime);

            _scopeTracerViewportPosition = new Vector2(
                Mathf.Clamp01(
                    _scopeTracerViewportPosition.x),
                Mathf.Clamp01(
                    _scopeTracerViewportPosition.y));

            _scopeTracerDepthOffset = Mathf.Max(
                0f,
                _scopeTracerDepthOffset);
        }
    }
}
