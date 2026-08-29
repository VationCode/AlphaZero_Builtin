using Alpha.Item.Weapon.Range;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.Item.Weapon.View
{
    // 관통 설정의 Radius와 동기화된 전용 LineRenderer로 경로를 표현한다.
    public sealed class PenetrationEffectView : MonoBehaviour
    {
        [SerializeField]
        private RangeAttackModule _attackModule;

        [SerializeField]
        private Material _material;

        [SerializeField, Min(0.01f)]
        private float _visibleDuration = 0.05f;

        private LineRenderer _lineRenderer;
        private float _hideTime;

        private void Awake()
        {
            _attackModule ??= GetComponent<RangeAttackModule>();
            EnsureLineRenderer();
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

            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (_lineRenderer != null &&
                _lineRenderer.enabled &&
                Time.time >= _hideTime)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void HandleTrajectoryResolved(
            RangeAttackResult p_result)
        {
            EnsureLineRenderer();
            SyncRendererWidth();

            _lineRenderer.SetPosition(
                0,
                p_result.StartPoint);
            _lineRenderer.SetPosition(
                1,
                p_result.EndPoint);

            _lineRenderer.enabled = true;
            _hideTime = Time.time + _visibleDuration;
        }

        private void EnsureLineRenderer()
        {
            if (_lineRenderer != null)
                return;

            GameObject lineObject = new("PenetrationRenderer");
            lineObject.transform.SetParent(transform, false);
            lineObject.layer = gameObject.layer;

            _lineRenderer =
                lineObject.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.shadowCastingMode =
                ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.sharedMaterial = _material;
            SyncRendererWidth();
            _lineRenderer.enabled = false;
        }

        // 요청한 동일 수치 기준으로 공격 Radius를 Renderer Width에 그대로 적용한다.
        private void SyncRendererWidth()
        {
            if (_lineRenderer == null ||
                _attackModule == null)
            {
                return;
            }

            _lineRenderer.startWidth =
                _attackModule.StartRadius;
            _lineRenderer.endWidth =
                _attackModule.EndRadius;
        }

        private void OnValidate()
        {
            _visibleDuration = Mathf.Max(
                0.01f,
                _visibleDuration);

            SyncRendererWidth();
        }
    }
}
