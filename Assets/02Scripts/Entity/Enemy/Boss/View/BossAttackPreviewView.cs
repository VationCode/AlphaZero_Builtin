using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Boss
{
    // Inspector에 직접 입력한 값만 그린다. 패턴 에셋·전투 로직·피해 판정과 연결하지 않는다.
    [DisallowMultipleComponent]
    [AddComponentMenu("Alpha/Boss/Boss Attack Preview")]
    public sealed class BossAttackPreviewView : MonoBehaviour
    {
        public enum AttackForm { Melee, Projectile, Rush, Leap, Area }
        public enum AreaShape { Circle, Sector, Box }

        [Header("Preview Only - independent of gameplay")]
        [SerializeField, Tooltip("실제 보스 설정과 무관한 설계용 미리보기입니다.")]
        private bool _showPreview = true;
        [SerializeField, Tooltip("켜면 선택하지 않아도 표시합니다. Scene 뷰의 Gizmos도 켜야 합니다.")]
        private bool _alwaysVisible;
        [SerializeField] private bool _showLabels = true;
        [SerializeField, Tooltip("미리보기 위치·방향의 기준입니다. 비어 있으면 이 객체를 사용합니다. 크기 배율은 적용하지 않습니다.")]
        private Transform _origin;
        [SerializeField, Tooltip("기준 위치에서 표시 평면을 올릴 월드 높이입니다. 바닥과 겹치면 조절하세요.")]
        private float _heightOffset = 0.1f;
        [SerializeField, Tooltip("기준의 수평 전방에서 회전할 각도입니다.")]
        private float _directionAngle;

        [Header("Distances (world units)")]
        [SerializeField] private bool _showDistances = true;
        [SerializeField, Min(0f), Tooltip("직접 입력하는 추적 거리 미리보기입니다. 실제 감지·추적 설정을 변경하지 않습니다.")]
        private float _chaseDistance = 30f;
        [SerializeField, Min(0f), Tooltip("직접 입력하는 공격 시작 거리 미리보기입니다.")]
        private float _attackStartDistance = 20f;

        [Header("Attack Form and Area")]
        [SerializeField] private bool _showAttackArea = true;
        [SerializeField, Tooltip("공격 표현 형태입니다. 실제 패턴 타입이나 거리와 연결되지 않습니다.")]
        private AttackForm _attackForm;
        [SerializeField, Tooltip("공격 형태와 독립적으로 선택하는 판정 영역 모양입니다.")]
        private AreaShape _areaShape;
        [SerializeField, Tooltip("공격 영역의 로컬 오프셋입니다. Projectile·Rush·Leap에서는 경로 끝점을 기준으로 적용합니다.")]
        private Vector3 _areaOffset;
        [SerializeField, Min(0f), Tooltip("Circle·Sector에 사용하는 반경입니다.")]
        private float _radius = 2f;
        [SerializeField, Min(0f), Tooltip("Circle·Sector의 전체 높이입니다. 영역 중심에서 위·아래로 절반씩 표시하며, 0이면 평면으로 표시합니다. Box는 Box Size의 Y를 사용합니다.")]
        private float _areaHeight = 2f;
        [SerializeField, Range(0f, 360f), Tooltip("Sector의 전체 각도입니다.")]
        private float _sectorAngle = 90f;
        [SerializeField, Tooltip("Box의 폭(X)·높이(Y)·길이(Z)입니다. 오프셋 위치를 중심으로 그립니다.")]
        private Vector3 _boxSize = new(4f, 2f, 6f);

        [Header("Path (Projectile / Rush / Leap)")]
        [SerializeField, Min(0f), Tooltip("전방으로 표시할 발사·돌진·도약 거리입니다.")]
        private float _pathDistance = 20f;
        [SerializeField, Min(0f), Tooltip("Rush 경로의 폭입니다. 끝점의 공격 영역 크기와 별개입니다.")]
        private float _rushWidth = 4f;
        [SerializeField, Min(0f), Tooltip("Leap 경로 중간의 최고 높이입니다.")]
        private float _leapHeight = 4f;

        [Header("Colors")]
        [SerializeField] private Color _chaseColor = new(0.15f, 0.6f, 1f, 1f);
        [SerializeField] private Color _startColor = new(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color _areaColor = new(1f, 0.2f, 0.15f, 1f);

        private void OnValidate()
        {
            _chaseDistance = NonNegative(_chaseDistance);
            _attackStartDistance = NonNegative(_attackStartDistance);
            _radius = NonNegative(_radius);
            _areaHeight = NonNegative(_areaHeight);
            _sectorAngle = Mathf.Clamp(Finite(_sectorAngle), 0f, 360f);
            _boxSize = new Vector3(NonNegative(_boxSize.x), NonNegative(_boxSize.y), NonNegative(_boxSize.z));
            _areaOffset = new Vector3(Finite(_areaOffset.x), Finite(_areaOffset.y), Finite(_areaOffset.z));
            _pathDistance = NonNegative(_pathDistance);
            _rushWidth = NonNegative(_rushWidth);
            _leapHeight = NonNegative(_leapHeight);
            _heightOffset = Finite(_heightOffset);
            _directionAngle = Finite(_directionAngle);
        }

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        private static float NonNegative(float value) => Mathf.Max(0f, Finite(value));

#if UNITY_EDITOR
        private void OnDrawGizmos() { if (_alwaysVisible) DrawPreview(); }
        private void OnDrawGizmosSelected() { if (!_alwaysVisible) DrawPreview(); }

        private void DrawPreview()
        {
            if (!_showPreview || !enabled) return;
            Transform origin = _origin != null ? _origin : transform;
            Vector3 center = origin.position + Vector3.up * _heightOffset;
            Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward = Quaternion.AngleAxis(_directionAngle, Vector3.up) * forward.normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Color oldHandlesColor = Handles.color, oldGizmosColor = Gizmos.color;
            Matrix4x4 oldHandlesMatrix = Handles.matrix, oldGizmosMatrix = Gizmos.matrix;
            try
            {
                Handles.matrix = Gizmos.matrix = Matrix4x4.identity;
                if (_showDistances)
                {
                    DrawDistance(center, forward, _chaseDistance, _chaseColor, "Chase");
                    DrawDistance(center, forward, _attackStartDistance, _startColor, "Attack Start");
                }
                if (_showAttackArea)
                {
                    Handles.color = _areaColor;
                    Vector3 areaCenter = DrawPath(center, forward, rotation) + rotation * _areaOffset;
                    DrawArea(areaCenter, forward, rotation);
                    if (_showLabels) Handles.Label(areaCenter + Vector3.up * 0.5f, $"{_attackForm} / {_areaShape} (preview)");
                }
                if (_showLabels)
                {
                    Handles.color = Color.white;
                    Handles.Label(center + Vector3.up, "Boss Preview - manual values");
                }
            }
            finally
            {
                Handles.color = oldHandlesColor;
                Handles.matrix = oldHandlesMatrix;
                Gizmos.color = oldGizmosColor;
                Gizmos.matrix = oldGizmosMatrix;
            }
        }

        private void DrawDistance(Vector3 center, Vector3 forward, float radius, Color color, string label)
        {
            Handles.color = color;
            if (radius > 0f) Handles.DrawWireDisc(center, Vector3.up, radius);
            if (_showLabels) Handles.Label(center + forward * radius, $"{label}: {radius:0.##} m");
        }

        // 이동 경로는 실제 시뮬레이션 없이 그린다. 도약 곡선의 시작·종점 높이는 같다.
        private Vector3 DrawPath(Vector3 start, Vector3 forward, Quaternion rotation)
        {
            if (_attackForm is AttackForm.Melee or AttackForm.Area) return start;
            Vector3 end = start + forward * _pathDistance;
            if (_attackForm == AttackForm.Leap)
            {
                Vector3 previous = start;
                for (int i = 1; i <= 40; i++)
                {
                    float t = i / 40f;
                    Vector3 point = Vector3.Lerp(start, end, t) + Vector3.up * (4f * _leapHeight * t * (1f - t));
                    Handles.DrawLine(previous, point);
                    previous = point;
                }
                Handles.DrawDottedLine(start, end, 4f);
            }
            else
            {
                Handles.DrawLine(start, end);
                if (_attackForm == AttackForm.Rush)
                {
                    Vector3 side = rotation * Vector3.right * (_rushWidth * 0.5f);
                    Handles.DrawLine(start - side, end - side);
                    Handles.DrawLine(start + side, end + side);
                    Handles.DrawLine(start - side, start + side);
                    Handles.DrawLine(end - side, end + side);
                }
            }
            float arrowSize = Mathf.Min(1f, _pathDistance * 0.2f);
            Vector3 right = rotation * Vector3.right * (arrowSize * 0.5f);
            Handles.DrawLine(end, end - forward * arrowSize + right);
            Handles.DrawLine(end, end - forward * arrowSize - right);
            if (_showLabels) Handles.Label((start + end) * 0.5f, $"Path: {_pathDistance:0.##} m");
            return end;
        }

        private void DrawArea(Vector3 center, Vector3 forward, Quaternion rotation)
        {
            if (_areaShape == AreaShape.Box)
            {
                Gizmos.color = _areaColor;
                Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, _boxSize);
                if (_showLabels) Handles.Label(center + Vector3.up * (_boxSize.y * 0.5f), $"Size: {_boxSize.x:0.##} x {_boxSize.y:0.##} x {_boxSize.z:0.##} m");
                return;
            }
            float angle = _areaShape == AreaShape.Circle ? 360f : _sectorAngle;
            Vector3 edge = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward;
            Vector3 halfHeight = Vector3.up * (_areaHeight * 0.5f);
            Vector3 bottom = center - halfHeight, top = center + halfHeight;
            DrawAreaFace(bottom, edge, angle);
            if (_areaHeight > 0f)
            {
                DrawAreaFace(top, edge, angle);
                // 원은 원기둥, 부채꼴은 부채꼴 기둥으로 표시한다.
                Handles.color = _areaColor;
                if (_radius > 0f)
                {
                    int segments = Mathf.Max(1, Mathf.CeilToInt(angle / 45f));
                    int lastEdge = angle < 360f ? segments : segments - 1;
                    for (int i = 0; i <= lastEdge; i++)
                    {
                        Vector3 offset = Quaternion.AngleAxis(angle * i / segments, Vector3.up) * edge * _radius;
                        Handles.DrawLine(bottom + offset, top + offset);
                    }
                }
                // 부채꼴 양쪽의 방사형 면은 중심의 세로 모서리도 공유한다.
                if (angle < 360f || _radius <= 0f) Handles.DrawLine(bottom, top);
            }
            if (_showLabels) Handles.Label(top + forward * _radius,
                $"Radius: {_radius:0.##} m / Height: {_areaHeight:0.##} m / {angle:0.#} deg");
        }

        private void DrawAreaFace(Vector3 center, Vector3 edge, float angle)
        {
            Color fill = _areaColor;
            fill.a *= 0.12f;
            Handles.color = fill;
            if (_radius > 0f && angle > 0f) Handles.DrawSolidArc(center, Vector3.up, edge, angle, _radius);
            Handles.color = _areaColor;
            if (_radius > 0f) Handles.DrawWireArc(center, Vector3.up, edge, angle, _radius);
            if (angle < 360f)
            {
                Handles.DrawLine(center, center + edge * _radius);
                Handles.DrawLine(center, center + Quaternion.AngleAxis(angle, Vector3.up) * edge * _radius);
            }
        }
#endif
    }
}
