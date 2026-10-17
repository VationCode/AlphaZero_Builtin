using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // 공격별 범위·피해와 Attack6 소환을 실행한다. 실행 시점은 Flow가 결정한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossAttackModule : MonoBehaviour
    {
        private const int HitBufferCapacity = 128;

        [Header("Attack 1: Forward Claw Swing")]
        [Tooltip("Shape은 Forward Sector로 유지하고 Target Layers를 피격 대상에 맞게 설정합니다.")]
        [SerializeField]
        private DetectionAreaSettings _attack1Area = CreateDefaultAttack1Area();
        [Tooltip("Attack1 예고와 채움에 공통으로 사용하는 RGB 색상입니다. 알파는 아래 값으로 설정합니다.")]
        [SerializeField] private Color _attack1WarningColor = new(1f, 0.3f, 0.08f, 1f);
        [Tooltip("Attack1 예고의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack1WarningAlpha = 80;
        [Tooltip("Attack1 채움의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack1FillAlpha = 150;
        [Tooltip("Once는 HitStart에서 한 번, Continuous는 Hit Duration 동안 판정합니다.")]
        [SerializeField] private CrabBossHitMode _attack1HitMode = CrabBossHitMode.Once;
        [Tooltip("HitStart 이후 판정을 유지할 클립 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float _attack1HitDurationSeconds = 0.2f;
        [Tooltip("Attack1 피해 범위를 Scene 뷰에 표시합니다.")]
        [SerializeField] private bool _showAttack1HitArea = true;
        [SerializeField] private Color _attack1HitAreaColor = new(1f, 0.3f, 0.08f, 0.12f);
        [Tooltip("Scene 뷰에서 Attack1 피해 범위 내부의 불투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float _attack1HitAreaFillOpacity = 0.12f;

        [SerializeField]
        private DamageProfile _attack1Damage = new();

        [Header("Attack 2: Targeted Area")]
        [Tooltip("Shape은 Radial로 유지합니다. 공격 위치는 Flow가 고정해 전달합니다.")]
        [SerializeField]
        private DetectionAreaSettings _attack2Area = CreateDefaultAttack2Area();
        [Tooltip("Attack2 예고와 채움에 공통으로 사용하는 RGB 색상입니다. 알파는 아래 값으로 설정합니다.")]
        [SerializeField] private Color _attack2WarningColor = new(0.35f, 0.45f, 1f, 1f);
        [Tooltip("Attack2 예고의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack2WarningAlpha = 80;
        [Tooltip("Attack2 채움의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack2FillAlpha = 150;
        [Tooltip("Once는 HitStart에서 한 번, Continuous는 Hit Duration 동안 판정합니다.")]
        [SerializeField] private CrabBossHitMode _attack2HitMode = CrabBossHitMode.Once;
        [Tooltip("HitStart 이후 판정을 유지할 클립 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float _attack2HitDurationSeconds = 0.2f;
        [Tooltip("Attack2 피해 범위를 Scene 뷰에 표시합니다.")]
        [SerializeField] private bool _showAttack2HitArea = true;
        [SerializeField] private Color _attack2HitAreaColor = new(0.15f, 0.65f, 1f, 0.7f);
        [Tooltip("Scene 뷰에서 Attack2 피해 범위 내부의 불투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float _attack2HitAreaFillOpacity = 0.08f;

        [SerializeField]
        private DamageProfile _attack2Damage = new();

        [Header("Attack 3: Jump Landing")]
        [Tooltip("착지 피해와 예고 표시가 공유하는 범위입니다.")]
        [SerializeField] private DetectionAreaSettings _attack3Area =
            CreateDefaultAttack3Area();
        [Tooltip("Attack3 예고와 채움에 공통으로 사용하는 RGB 색상입니다. 알파는 아래 값으로 설정합니다.")]
        [SerializeField] private Color _attack3WarningColor = new(1f, 0.35f, 0.05f, 1f);
        [Tooltip("Attack3 예고의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack3WarningAlpha = 80;
        [Tooltip("Attack3 채움의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack3FillAlpha = 150;
        [Tooltip("Once는 HitStart에서 한 번, Continuous는 Hit Duration 동안 판정합니다.")]
        [SerializeField] private CrabBossHitMode _attack3HitMode = CrabBossHitMode.Once;
        [Tooltip("HitStart 이후 판정을 유지할 클립 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float _attack3HitDurationSeconds = 0.2f;
        [Tooltip("Attack3 피해 범위를 Scene 뷰에 표시합니다.")]
        [SerializeField] private bool _showAttack3HitArea = true;
        [SerializeField] private Color _attack3HitAreaColor = new(0.75f, 0.35f, 1f, 0.7f);
        [Tooltip("Scene 뷰에서 Attack3 피해 범위 내부의 불투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float _attack3HitAreaFillOpacity = 0.08f;

        [SerializeField]
        private DamageProfile _attack3Damage = new();

        [Header("Attack 4: Forward Dash")]
        [Tooltip("Forward Box의 Length가 돌진 거리와 피해 판정 길이를 결정합니다.")]
        [SerializeField] private DetectionAreaSettings _attack4Area = CreateDefaultAttack4Area();
        [Tooltip("HitStart 이후 돌진을 완료할 애니메이션 클립 시간(초)입니다.")]
        [SerializeField, Min(0.01f)] private float _attack4DashDurationSeconds = 0.3f;
        [Tooltip("Attack4 예고와 채움에 공통으로 사용하는 RGB 색상입니다. 알파는 아래 값으로 설정합니다.")]
        [SerializeField] private Color _attack4WarningColor = new(1f, 0.4f, 0.05f, 1f);
        [Tooltip("Attack4 예고의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack4WarningAlpha = 80;
        [Tooltip("Attack4 채움의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack4FillAlpha = 150;
        [Tooltip("Attack4 피해 범위를 Scene 뷰에 표시합니다.")]
        [SerializeField] private bool _showAttack4HitArea = true;
        [SerializeField] private Color _attack4HitAreaColor = new(1f, 0.75f, 0.1f, 0.7f);
        [Tooltip("Scene 뷰에서 Attack4 피해 범위 내부의 불투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float _attack4HitAreaFillOpacity = 0.25f;
        [SerializeField] private DamageProfile _attack4Damage = new();

        [Header("Attack 5: Three Projectile Volleys")]
        [Tooltip("EffectView의 Spawn Point를 기준으로 표시하는 투사체 경로입니다. Length는 충돌 종료 모드의 최대 사거리입니다.")]
        [SerializeField] private DetectionAreaSettings _attack5Area =
            CreateDefaultAttack5Area();
        [Tooltip("Attack5 경로 예고와 채움에 공통으로 사용하는 RGB 색상입니다. 알파는 아래 값으로 설정합니다.")]
        [SerializeField] private Color _attack5WarningColor =
            new(1f, 0.19607843f, 0.19607843f, 1f);
        [Tooltip("Attack5 경로 예고의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack5WarningAlpha = 80;
        [Tooltip("Attack5 경로가 채워질 때의 알파값입니다 (0~255).")]
        [SerializeField, Range(0, 255)] private int _attack5FillAlpha = 150;
        [Tooltip("Attack5 투사체 경로를 Scene 뷰에 표시합니다.")]
        [SerializeField] private bool _showAttack5Areas = true;
        [SerializeField] private Color _attack5AreaColor =
            new(0.3f, 1f, 0.5f, 0.7f);
        [SerializeField, Range(0f, 1f)] private float _attack5AreaFillOpacity = 0.12f;

        [Header("Attack 6: Mini Crab Summon")]
        [Tooltip("비워두면 전투 시작 위치와 정면으로 점프해 돌아갑니다.")]
        [SerializeField] private Transform _attack6JumpTarget;
        [SerializeField] private MiniCrabAgent _attack6MiniCrabPrefab;
        [SerializeField, Min(1)] private int _attack6SummonCount = 5;
        [Tooltip("타깃을 중심으로 소환 위치를 정하고 Scene 뷰에 표시할 범위입니다.")]
        [SerializeField] private DetectionAreaSettings _attack6Area =
            CreateDefaultAttack6Area();
        [SerializeField, Min(0f)] private float _attack6MinimumSpawnRadius = 5f;
        [Tooltip("소환된 미니 게 중심 사이의 최소 수평 거리입니다.")]
        [SerializeField, Min(0f)] private float _attack6MinimumCrabSpacing = 2.5f;
        [SerializeField] private LayerMask _attack6GroundLayers = 1 << 7;
        [SerializeField] private Color _attack6WarningColor = new(0.75f, 0.25f, 1f, 1f);
        [SerializeField, Range(0, 255)] private int _attack6WarningAlpha = 80;
        [SerializeField, Range(0, 255)] private int _attack6FillAlpha = 150;
        [SerializeField] private bool _showAttack6Area = true;
        [SerializeField] private Color _attack6AreaColor = new(0.75f, 0.25f, 1f, 0.7f);
        [SerializeField, Range(0f, 1f)] private float _attack6AreaFillOpacity = 0.12f;

        internal Color Attack1WarningColor => WithAlpha(_attack1WarningColor, _attack1WarningAlpha);
        internal Color Attack1FillColor => WithAlpha(_attack1WarningColor, _attack1FillAlpha);
        internal Color Attack2WarningColor => WithAlpha(_attack2WarningColor, _attack2WarningAlpha);
        internal Color Attack2FillColor => WithAlpha(_attack2WarningColor, _attack2FillAlpha);
        internal Color Attack3WarningColor => WithAlpha(_attack3WarningColor, _attack3WarningAlpha);
        internal Color Attack3FillColor => WithAlpha(_attack3WarningColor, _attack3FillAlpha);
        internal Color Attack4WarningColor => WithAlpha(_attack4WarningColor, _attack4WarningAlpha);
        internal Color Attack4FillColor => WithAlpha(_attack4WarningColor, _attack4FillAlpha);
        internal Color Attack5WarningColor => WithAlpha(_attack5WarningColor, _attack5WarningAlpha);
        internal Color Attack5FillColor => WithAlpha(_attack5WarningColor, _attack5FillAlpha);
        internal Color Attack6WarningColor => WithAlpha(_attack6WarningColor, _attack6WarningAlpha);
        internal Color Attack6FillColor => WithAlpha(_attack6WarningColor, _attack6FillAlpha);
        internal bool IsAttack6Ready => _attack6MiniCrabPrefab != null &&
            _attack6GroundLayers.value != 0 && _attack6Area != null &&
            _attack6Area.Shape == EDetectionAreaShape.Radial && _attack6Area.IsValid;
        internal Vector3 GetAttack6JumpPosition(Vector3 fallback) =>
            _attack6JumpTarget != null ? _attack6JumpTarget.position : fallback;
        internal Vector3 GetAttack6JumpForward(Vector3 fallback) =>
            _attack6JumpTarget != null ? _attack6JumpTarget.forward : fallback;

        private static Color WithAlpha(Color color, int alpha)
        {
            color.a = Mathf.Clamp(alpha, 0, 255) / 255f;
            return color;
        }
        internal CrabBossHitTiming Attack1HitTiming =>
            new(_attack1HitMode, _attack1HitDurationSeconds);
        internal CrabBossHitTiming Attack2HitTiming =>
            new(_attack2HitMode, _attack2HitDurationSeconds);
        internal CrabBossHitTiming Attack3HitTiming =>
            new(_attack3HitMode, _attack3HitDurationSeconds);
        internal CrabBossHitTiming Attack4HitTiming => new(CrabBossHitMode.Once, 0f);
        internal float Attack4DashDurationSeconds => _attack4DashDurationSeconds;

        internal bool IsAttack5AreaConfigured =>
            _attack5Area != null &&
            _attack5Area.Shape == EDetectionAreaShape.ForwardBox &&
            _attack5Area.IsValid;
        internal float Attack5Range => IsAttack5AreaConfigured ? _attack5Area.Length : 0f;

        // EffectView가 실제 생성에 사용할 자세를 전달한다.
        internal bool TryGetAttack5Area(in Pose spawnPose,
            out DetectionAreaRequest request)
        {
            request = default;
            if (!IsAttack5AreaConfigured)
                return false;

            request = new DetectionAreaRequest(spawnPose.position,
                spawnPose.rotation * Vector3.forward,
                spawnPose.rotation * Vector3.up, Owner, _attack5Area);
            return request.IsValid;
        }

#if UNITY_EDITOR
        internal bool ShowAttack1HitArea => _showAttack1HitArea;
        internal Color Attack1HitAreaColor => _attack1HitAreaColor;
        internal float Attack1HitAreaFillOpacity => _attack1HitAreaFillOpacity;
        internal bool ShowAttack2HitArea => _showAttack2HitArea;
        internal Color Attack2HitAreaColor => _attack2HitAreaColor;
        internal float Attack2HitAreaFillOpacity => _attack2HitAreaFillOpacity;
        internal bool ShowAttack3HitArea => _showAttack3HitArea;
        internal Color Attack3HitAreaColor => _attack3HitAreaColor;
        internal float Attack3HitAreaFillOpacity => _attack3HitAreaFillOpacity;
        internal bool ShowAttack4HitArea => _showAttack4HitArea;
        internal Color Attack4HitAreaColor => _attack4HitAreaColor;
        internal float Attack4HitAreaFillOpacity => _attack4HitAreaFillOpacity;
        internal bool ShowAttack5Areas => _showAttack5Areas;
        internal Color Attack5AreaColor => _attack5AreaColor;
        internal float Attack5AreaFillOpacity => _attack5AreaFillOpacity;
        internal bool ShowAttack6Area => _showAttack6Area;
        internal Color Attack6AreaColor => _attack6AreaColor;
        internal float Attack6AreaFillOpacity => _attack6AreaFillOpacity;
        internal float Attack6MinimumSpawnRadius => _attack6MinimumSpawnRadius;
#endif

        private readonly Collider[] _overlapBuffer = new Collider[HitBufferCapacity];
        private readonly DetectionAreaHit[] _hitBuffer = new DetectionAreaHit[HitBufferCapacity];
        // 같은 범위 검사에서 대상의 여러 Collider가 중복 피해를 주지 않도록 한다.
        private readonly HashSet<IDamageable> _sampledTargets = new();
        private readonly List<Vector3> _attack6SpawnPositions = new();

        private Transform _owner;
        private Transform _attack1Origin;
        private bool _hitWindowActive;

        // Attack3의 실제 충격 설정과 같은 거리/시간으로 접촉 해소 속도를 계산한다.
        public bool TryGetAttack3KnockbackSpeed(out float speed)
        {
            speed = 0f;
            if (_attack3Damage == null)
                return false;

            AttackImpactInfo impact = _attack3Damage.Impact;
            if (impact.KnockbackDistance <= 0f || impact.KnockbackDuration <= 0f)
                return false;

            speed = impact.KnockbackDistance / impact.KnockbackDuration;
            return true;
        }

        // Core가 보스 루트와 Attack1 판정 기준점을 연결한다.
        public void Bind(Transform owner, Transform attackOrigin)
        {
            EndHitWindow();
            ClearAttack6Plan();
            _owner = owner != null ? owner : transform;
            _attack1Origin = attackOrigin != null ? attackOrigin : _owner;
        }

        public void BeginHitWindow()
        {
            _sampledTargets.Clear();
            _hitWindowActive = true;
        }

        public void EndHitWindow()
        {
            _hitWindowActive = false;
            _sampledTargets.Clear();
        }

        public bool TryGetAttack6Area(Vector3 targetPosition,
            out DetectionAreaRequest request)
        {
            request = default;
            if (_attack6Area == null || _attack6Area.Shape != EDetectionAreaShape.Radial ||
                !_attack6Area.IsValid)
                return false;
            request = new DetectionAreaRequest(targetPosition, Vector3.forward,
                Vector3.up, Owner, _attack6Area);
            return request.IsValid;
        }

        // IndicatorStart에 소환 위치를 고정한다. 지면 높이는 실제 충돌면에서 얻는다.
        public void PlanAttack6(Vector3 targetPosition)
        {
            ClearAttack6Plan();
            if (!IsAttack6Ready || !TryGetAttack6Area(targetPosition,
                    out DetectionAreaRequest area))
                return;

            float outerRadius = _attack6Area.Radius;
            float innerRadius = Mathf.Min(_attack6MinimumSpawnRadius, outerRadius);
            float spacingSquared = _attack6MinimumCrabSpacing * _attack6MinimumCrabSpacing;
            Vector3 center = area.AreaOrigin;
            for (int index = 0; index < _attack6SummonCount; index++)
            {
                bool found = false;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    float radius = Mathf.Sqrt(Random.Range(innerRadius * innerRadius,
                        outerRadius * outerRadius));
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    if (direction.sqrMagnitude < 0.0001f)
                        direction = Vector2.right;
                    Vector3 probe = center + new Vector3(direction.x * radius,
                        20f, direction.y * radius);
                    if (!Physics.Raycast(probe, Vector3.down, out RaycastHit ground,
                            100f, _attack6GroundLayers, QueryTriggerInteraction.Ignore))
                        continue;
                    bool spaced = true;
                    foreach (Vector3 existing in _attack6SpawnPositions)
                    {
                        Vector2 difference = new(ground.point.x - existing.x,
                            ground.point.z - existing.z);
                        if (difference.sqrMagnitude >= spacingSquared)
                            continue;
                        spaced = false;
                        break;
                    }
                    if (!spaced)
                        continue;
                    _attack6SpawnPositions.Add(ground.point);
                    found = true;
                    break;
                }
                if (!found)
                    Debug.LogWarning("CrabBossAttackModule: Attack6 소환 지면을 찾지 못했습니다.", this);
            }
        }

        public void SummonAttack6()
        {
            if (!IsAttack6Ready)
                return;
            Collider[] bossColliders = Owner.GetComponentsInChildren<Collider>(true);
            foreach (Vector3 position in _attack6SpawnPositions)
            {
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                MiniCrabAgent crab = Instantiate(_attack6MiniCrabPrefab, position, rotation);
                crab.Initialize(Owner, bossColliders);
            }
            ClearAttack6Plan();
        }

        public void ClearAttack6Plan() => _attack6SpawnPositions.Clear();

        // View도 실제 타격과 같은 원점, 방향, 범위 설정을 읽는다.
        public bool TryGetAttack1Area(out DetectionAreaRequest request)
        {
            if (!IsAttack1Configured())
            {
                request = default;
                return false;
            }

            request = new DetectionAreaRequest(
                AttackOrigin.position,
                AttackOrigin.forward,
                AttackOrigin.up,
                Owner,
                _attack1Area);

            return request.IsValid;
        }

        public bool TryGetAttack2Area(Vector3 targetPosition,
            out DetectionAreaRequest request)
        {
            if (!IsAttack2Configured())
            {
                request = default;
                return false;
            }

            request = new DetectionAreaRequest(
                targetPosition, Owner.forward, Vector3.up, Owner, _attack2Area);
            return request.IsValid;
        }

        public bool TryGetAttack3Area(Vector3 landingPosition,
            out DetectionAreaRequest request)
        {
            if (!IsAttack3Configured())
            {
                request = default;
                return false;
            }

            request = new DetectionAreaRequest(
                landingPosition, Owner.forward, Vector3.up, Owner, _attack3Area);
            return request.IsValid;
        }

        // 타깃 방향을 고정하고 설정된 Length로 돌진 경로의 전방 판정 범위를 만든다.
        public bool TryGetAttack4Area(Vector3 start, Vector3 aimPosition,
            out DetectionAreaRequest request)
        {
            request = default;
            Vector3 direction = aimPosition - start;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f || _attack4Area == null ||
                _attack4Area.Shape != EDetectionAreaShape.ForwardBox ||
                !_attack4Area.IsValid || _attack4Area.TargetLayers.value == 0 ||
                _attack4Damage == null || !_attack4Damage.IsValid)
                return false;

            request = new DetectionAreaRequest(start, direction, Vector3.up, Owner,
                _attack4Area);
            return request.IsValid;
        }

        // 열린 타격 구간에서 매 검사마다 판정한다. 재피격 여부는 대상이 결정한다.
        public void ApplyAttack1()
        {
            if (!_hitWindowActive || !IsAttack1Configured())
                return;

            int hitCount = CollectAttack1Hits();
            _sampledTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(hit.Collider, out IDamageable damageable) ||
                    !_sampledTargets.Add(damageable))
                {
                    continue;
                }

                DamageInfo damageInfo = new(
                    Owner,
                    _attack1Damage.Damage,
                    hit.HitPoint,
                    -hit.Direction,
                    AttackOrigin.forward,
                    p_impact: _attack1Damage.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                DamageSystem.TryApply(hit.Collider, damageInfo);
            }
        }

        // Attack2는 IndicatorStart에서 고정한 위치를 구간 내내 사용한다.
        public void ApplyAttack2(Vector3 targetPosition)
        {
            if (!_hitWindowActive ||
                !TryGetAttack2Area(targetPosition, out DetectionAreaRequest request))
                return;

            ApplyAreaAttack(request, _attack2Damage, EDamageDeliveryType.Ranged);
        }

        public void ApplyAttack3(Vector3 landingPosition)
        {
            if (!_hitWindowActive ||
                !TryGetAttack3Area(landingPosition, out DetectionAreaRequest request))
                return;

            ApplyAreaAttack(request, _attack3Damage, EDamageDeliveryType.Melee);
        }

        public void ApplyAttack4(in DetectionAreaRequest request)
        {
            if (!_hitWindowActive || !request.IsValid)
                return;

            ApplyAreaAttack(request, _attack4Damage, EDamageDeliveryType.Melee,
                useLateralKnockback: true);
        }

        private void ApplyAreaAttack(in DetectionAreaRequest request,
            DamageProfile damage, EDamageDeliveryType deliveryType,
            bool useLateralKnockback = false)
        {
            Physics.SyncTransforms();
            int hitCount = DetectionAreaSystem.CollectHits(
                request, _overlapBuffer, _hitBuffer);
            _sampledTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(hit.Collider, out IDamageable damageable) ||
                    !_sampledTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 outward = Vector3.ProjectOnPlane(hit.Direction, Vector3.up);
                if (outward.sqrMagnitude <= 0.0001f)
                    outward = Owner.forward;
                if (useLateralKnockback)
                    outward = GetAttack4LateralDirection(request, hit);

                DamageInfo damageInfo = new(
                    Owner, damage.Damage, hit.HitPoint,
                    -hit.Direction, outward,
                    p_impact: damage.Impact,
                    p_deliveryType: deliveryType);

                DamageSystem.TryApply(hit.Collider, damageInfo);
            }
        }

        // 돌진 경로의 중심선에서 대상이 놓인 쪽으로만 넉백시킨다.
        private static Vector3 GetAttack4LateralDirection(
            in DetectionAreaRequest request, in DetectionAreaHit hit)
        {
            Vector3 forward = Vector3.ProjectOnPlane(request.Forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float side = Vector3.Dot(hit.Collider.bounds.center - request.AreaOrigin, right);
            if (Mathf.Abs(side) <= 0.01f)
                side = Vector3.Dot(hit.HitPoint - request.AreaOrigin, right);
            return side < 0f ? -right : right;
        }

        private Transform Owner => _owner != null ? _owner : transform;
        private Transform AttackOrigin => _attack1Origin != null ? _attack1Origin : Owner;

        private bool IsAttack1Configured() =>
            _attack1Area != null &&
            _attack1Area.Shape == EDetectionAreaShape.ForwardSector &&
            _attack1Area.IsValid &&
            _attack1Damage != null &&
            _attack1Damage.IsValid;

        private bool IsAttack2Configured() =>
            _attack2Area != null &&
            _attack2Area.Shape == EDetectionAreaShape.Radial &&
            _attack2Area.IsValid &&
            _attack2Damage != null &&
            _attack2Damage.IsValid;

        private bool IsAttack3Configured() =>
            _attack3Area != null &&
            (_attack3Area.Shape == EDetectionAreaShape.ForwardBox ||
             _attack3Area.Shape == EDetectionAreaShape.Radial) &&
            _attack3Area.IsValid &&
            _attack3Area.TargetLayers.value != 0 &&
            _attack3Damage != null &&
            _attack3Damage.IsValid;

        private int CollectAttack1Hits()
        {
            if (!TryGetAttack1Area(out DetectionAreaRequest request))
                return 0;

            Physics.SyncTransforms();

            return DetectionAreaSystem.CollectHits(
                request,
                _overlapBuffer,
                _hitBuffer);
        }

        private void OnValidate()
        {
            _attack1HitDurationSeconds = Mathf.Max(0f, _attack1HitDurationSeconds);
            _attack2HitDurationSeconds = Mathf.Max(0f, _attack2HitDurationSeconds);
            _attack3HitDurationSeconds = Mathf.Max(0f, _attack3HitDurationSeconds);
            _attack4DashDurationSeconds = Mathf.Max(0.01f, _attack4DashDurationSeconds);
            _attack6SummonCount = Mathf.Max(1, _attack6SummonCount);
            _attack1Area ??= CreateDefaultAttack1Area();
            _attack1Damage ??= new DamageProfile();
            _attack2Area ??= CreateDefaultAttack2Area();
            _attack2Damage ??= new DamageProfile();
            _attack3Area ??= CreateDefaultAttack3Area();
            _attack3Damage ??= new DamageProfile();
            _attack4Area ??= CreateDefaultAttack4Area();
            _attack4Damage ??= new DamageProfile();
            _attack5Area ??= CreateDefaultAttack5Area();
            _attack6Area ??= CreateDefaultAttack6Area();
            _attack1Area.Validate();
            _attack1Damage.Validate();
            _attack2Area.Validate();
            _attack2Damage.Validate();
            _attack3Area.Validate();
            _attack3Damage.Validate();
            _attack4Area.Validate();
            _attack4Damage.Validate();
            _attack5Area.Validate();
            _attack6Area.Validate();
            _attack6MinimumSpawnRadius = Mathf.Clamp(_attack6MinimumSpawnRadius,
                0f, _attack6Area.Radius);
            _attack6MinimumCrabSpacing = Mathf.Max(0f, _attack6MinimumCrabSpacing);
        }

        private static DetectionAreaSettings CreateDefaultAttack1Area() =>
            new(EDetectionAreaShape.ForwardSector, new Vector3(0f, 7.5f, 0f), 1 << 6,
                1f, 1f, 24f, 110f, 18f);

        private static DetectionAreaSettings CreateDefaultAttack2Area() =>
            new(EDetectionAreaShape.Radial, new Vector3(0f, 1.5f, 0f), 1 << 6,
                1f, 1f, 5f, 360f, 3f);

        private static DetectionAreaSettings CreateDefaultAttack3Area() =>
            new(EDetectionAreaShape.ForwardBox, new Vector3(0f, 0.6f, -9.31f),
                1 << 6, 14.5f, 17f, 1f, 360f, 1f);

        private static DetectionAreaSettings CreateDefaultAttack4Area() =>
            new(EDetectionAreaShape.ForwardBox, new Vector3(0f, 2f, 0f), 1 << 6,
                8f, 20f, 1f, 360f, 4f);

        private static DetectionAreaSettings CreateDefaultAttack5Area() =>
            new(EDetectionAreaShape.ForwardBox, Vector3.zero, 1 << 6,
                2f, 30f, 1f, 90f, 2f);

        private static DetectionAreaSettings CreateDefaultAttack6Area() =>
            new(EDetectionAreaShape.Radial, new Vector3(0f, 0.5f, 0f),
                1 << 6, 1f, 1f, 12f, 360f, 1f);
    }

    internal enum CrabBossHitMode
    {
        Once,
        Continuous
    }

    // Flow가 공격별 타격 창을 열 때 사용하는 읽기 전용 설정이다.
    internal readonly struct CrabBossHitTiming
    {
        internal readonly CrabBossHitMode Mode;
        internal readonly float DurationSeconds;

        internal CrabBossHitTiming(CrabBossHitMode mode, float durationSeconds)
        {
            Mode = mode;
            DurationSeconds = durationSeconds;
        }
    }
}
