using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Detection
{
    // 영역 감지를 실행하고 한 번의 감지 결과와 개수를 내부 버퍼에 보관한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DetectionAreaGizmoView))]
    public sealed class AreaDetectionModule : MonoBehaviour
    {
        private const int HitBufferCapacity = 32;

        [FormerlySerializedAs("_searchOrigin")]
        [FormerlySerializedAs("_detectionOrigin")]
        [SerializeField]
        private Transform _origin;

        [FormerlySerializedAs("_searchArea")]
        [FormerlySerializedAs("_detectionArea")]
        [SerializeField]
        private DetectionAreaSettings _settings = new();

        private readonly Collider[] _overlapBuffer =
            new Collider[HitBufferCapacity];

        private readonly DetectionAreaHit[] _hitBuffer =
            new DetectionAreaHit[HitBufferCapacity];

        private Transform _owner;

        public DetectionAreaSettings Settings => _settings;
        public int HitCount { get; private set; }

        // 설정된 Local Offset까지 반영한 현재 감지 영역의 중심점이다.
        public Vector3 AreaOrigin
        {
            get
            {
                DetectionAreaRequest request = CreateRequest();
                return request.AreaOrigin;
            }
        }

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
        }

        // Inspector에 설정된 영역으로 감지하고 결과 개수를 반환한다.
        public int CollectHits()
        {
            return CollectHits(CreateRequest());
        }

        // 공격처럼 실행 시점에 만들어지는 영역도 같은 결과 버퍼를 사용할 수 있다.
        public int CollectHits(in DetectionAreaRequest p_request)
        {
            if (!isActiveAndEnabled || !p_request.IsValid)
            {
                HitCount = 0;
                return 0;
            }

            HitCount = DetectionAreaSystem.CollectHits(
                p_request,
                _overlapBuffer,
                _hitBuffer);

            return HitCount;
        }

        public bool TryGetHit(
            int p_index,
            out DetectionAreaHit p_hit)
        {
            if (p_index < 0 || p_index >= HitCount)
            {
                p_hit = default;
                return false;
            }

            p_hit = _hitBuffer[p_index];
            return p_hit.IsValid;
        }

        // View가 실제 감지와 동일한 자세와 설정을 표현할 수 있도록 요청을 제공한다.
        public DetectionAreaRequest CreateRequest()
        {
            Transform origin = ResolveOrigin();
            Transform owner = _owner != null ? _owner : transform;

            return new DetectionAreaRequest(
                origin.position,
                origin.forward,
                origin.up,
                owner,
                _settings);
        }

        private Transform ResolveOrigin()
        {
            if (_origin != null)
                return _origin;

            return _owner != null ? _owner : transform;
        }

        private void OnValidate()
        {
            _settings ??= new DetectionAreaSettings();
            _settings.Validate();
        }

    }
}
