using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Alpha.Boss
{
    // 회차마다 현재 타겟 주변의 위치와 개수를 새로 선정하고 정지형 공격을 동시에 생성한다.
    public sealed class BossAreaAttackModule
    {
        private const int GroundAttemptsPerPoint = 64;
        private readonly System.Random _random;

        public BossAreaAttackModule() : this(new System.Random()) { }
        internal BossAreaAttackModule(System.Random p_random) =>
            _random = p_random ?? throw new ArgumentNullException(nameof(p_random));

        public bool IsConfigured(BossAreaAttackSettings p_settings) => p_settings != null &&
            p_settings.AttackPrefab != null && p_settings.AttackPrefab.IsConfigurationValid &&
            IsFinite(p_settings.SpawnRadius) && p_settings.SpawnRadius >= 0f &&
            p_settings.MinSpawnCount >= 1 && p_settings.MaxSpawnCount >= p_settings.MinSpawnCount &&
            IsFinite(p_settings.StartTimeSeconds) && p_settings.StartTimeSeconds >= 0f &&
            IsFinite(p_settings.DurationSeconds) && p_settings.DurationSeconds >= 0f &&
            p_settings.RepeatCount >= 1 && IsFinite(p_settings.GetSpawnTimeSeconds(p_settings.RepeatCount - 1)) &&
            IsFinite(p_settings.Lifetime) && p_settings.Lifetime > 0f &&
            p_settings.GroundMask.value != 0 && IsFinite(p_settings.GroundProbeHeight) &&
            p_settings.GroundProbeHeight > 0f && IsFinite(p_settings.GroundProbeDistance) &&
            p_settings.GroundProbeDistance >= p_settings.GroundProbeHeight;

        public bool TryCapturePositions(Transform p_owner, Transform p_target, BossAreaAttackSettings p_settings,
            out Vector3[] p_positions)
        {
            p_positions = Array.Empty<Vector3>();
            if (p_owner == null || p_target == null || !IsConfigured(p_settings))
                return false;
            Vector3 center = p_target.position;
            if (!IsFinite(center))
                return false;
            // double로 폭을 계산하여 Max가 int.MaxValue일 때도 Max + 1의 정수 오버플로를 피한다.
            int count = (int)(p_settings.MinSpawnCount + Math.Floor(_random.NextDouble() *
                ((double)p_settings.MaxSpawnCount - p_settings.MinSpawnCount + 1d)));
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                bool found = false;
                for (int attempt = 0; attempt < GroundAttemptsPerPoint; attempt++)
                {
                    double angle = _random.NextDouble() * Math.PI * 2d;
                    // sqrt를 사용하여 원판의 면적 전체에 균등하게 분포시킨다.
                    double radius = p_settings.SpawnRadius * Math.Sqrt(_random.NextDouble());
                    Vector3 sample = center + new Vector3((float)(Math.Cos(angle) * radius),
                        0f, (float)(Math.Sin(angle) * radius));
                    if (!TryFindGround(p_owner, p_target, p_settings, sample, out positions[i]))
                        continue;
                    found = true;
                    break;
                }
                // 지면 없는 후보는 다시 뽑는다. 정한 개수를 확보하지 못하면 일부만 생성하지 않는다.
                if (!found)
                    return false;
            }
            p_positions = positions;
            return true;
        }

        // Scene 미리보기와 실제 생성이 같은 지면 탐색 규칙을 사용한다.
        public static bool TryFindGround(Transform p_owner, Transform p_target, BossAreaAttackSettings p_settings,
            Vector3 p_sample, out Vector3 p_position)
        {
            p_position = default;
            if (p_settings == null || p_settings.GroundMask.value == 0 ||
                !IsFinite(p_settings.GroundProbeHeight) || p_settings.GroundProbeHeight <= 0f ||
                !IsFinite(p_settings.GroundProbeDistance) ||
                p_settings.GroundProbeDistance < p_settings.GroundProbeHeight)
                return false;
            Vector3 origin = p_sample + Vector3.up * p_settings.GroundProbeHeight;
            if (!IsFinite(origin))
                return false;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, p_settings.GroundProbeDistance,
                         p_settings.GroundMask, QueryTriggerInteraction.Ignore))
            {
                if ((p_owner != null && hit.transform.IsChildOf(p_owner)) ||
                    (p_target != null && hit.transform.IsChildOf(p_target)) || hit.distance >= nearest)
                    continue;
                nearest = hit.distance;
                p_position = hit.point;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        public bool Execute(Transform p_owner, Transform p_target, BossPatternData p_pattern)
        {
            if (!TryCapturePositions(p_owner, p_target, p_pattern?.AreaAttack, out Vector3[] positions))
            {
                if (p_owner != null)
                    Debug.LogWarning("Area 회차의 생성 위치를 확보하지 못했습니다. 타겟, 생성 반경, Ground Mask와 지면 탐색 거리를 확인하세요.", p_owner);
                return false;
            }
            return Execute(p_owner, p_pattern, positions);
        }

        public bool Execute(Transform p_owner, BossPatternData p_pattern, IReadOnlyList<Vector3> p_positions)
        {
            BossAreaAttackSettings settings = p_pattern?.AreaAttack;
            if (p_owner == null || !IsConfigured(settings) || p_positions == null || p_positions.Count == 0)
                return false;
            for (int i = 0; i < p_positions.Count; i++)
                if (!IsFinite(p_positions[i]))
                    return false;
            var attacks = new List<AOEAttack>(p_positions.Count);
            foreach (Vector3 position in p_positions)
            {
                var attack = Object.Instantiate(settings.AttackPrefab, position, settings.AttackPrefab.transform.rotation);
                attacks.Add(attack);
                if (attack.InitializeArea(p_owner, p_pattern.DamageProfile.Damage,
                        p_pattern.DamageProfile.Impact, settings.Lifetime))
                    continue;
                // 한 공격이라도 초기화에 실패하면 피해가 발생하기 전에 이번 배치를 모두 정리한다.
                foreach (var created in attacks)
                {
                    created.gameObject.SetActive(false);
                    Object.Destroy(created.gameObject);
                }
                return false;
            }
            foreach (var attack in attacks)
                attack.gameObject.SetActive(true);
            return true;
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
        private static bool IsFinite(Vector3 p_value) => IsFinite(p_value.x) && IsFinite(p_value.y) && IsFinite(p_value.z);
    }
}
