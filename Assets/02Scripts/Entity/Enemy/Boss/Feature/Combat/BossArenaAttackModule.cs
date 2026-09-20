using System.Collections.Generic;
using UnityEngine;
using ArenaAttackEntity = Alpha.Boss.AOEAttack;

namespace Alpha.Boss
{
    // 한 그룹의 지점과 방향을 먼저 저장한 뒤 이동형 공격 Entity들을 동시에 생성한다.
    public sealed class BossArenaAttackModule
    {
        public bool IsConfigured(BossArenaAttackSettings p_settings)
        {
            if (p_settings == null || p_settings.AttackPrefab == null ||
                !p_settings.AttackPrefab.IsConfigurationValid || !IsFinite(p_settings.MoveSpeed) ||
                p_settings.MoveSpeed <= 0f || !IsFinite(p_settings.MaximumDistance) ||
                p_settings.MaximumDistance <= 0f || p_settings.SpawnGroupCount == 0)
                return false;
            for (int i = 0; i < p_settings.SpawnGroupCount; i++)
            {
                BossArenaSpawnGroup group = p_settings.GetSpawnGroup(i);
                if (group == null || !IsFinite(group.SpawnTimeSeconds) || group.SpawnTimeSeconds < 0f ||
                    group.SpawnPointCount == 0)
                    return false;
                for (int j = 0; j < group.SpawnPointCount; j++)
                    if (group.GetSpawnPoint(j) == null)
                        return false;
            }
            return true;
        }

        public bool Execute(Transform p_owner, BossPatternData p_pattern, int p_groupIndex)
        {
            BossArenaAttackSettings settings = p_pattern?.ArenaAttack;
            if (p_owner == null || !IsConfigured(settings))
                return false;
            BossArenaSpawnGroup group = settings.GetSpawnGroup(p_groupIndex);
            if (group == null)
                return false;
            var poses = new Pose[group.SpawnPointCount];
            for (int i = 0; i < poses.Length; i++)
            {
                Transform point = group.GetSpawnPoint(i);
                poses[i] = new Pose(point.position, point.rotation);
            }
            var attacks = new List<ArenaAttackEntity>(poses.Length);
            foreach (Pose pose in poses)
            {
                var attack = Object.Instantiate(settings.AttackPrefab, pose.position, pose.rotation);
                attacks.Add(attack);
                if (!attack.InitializeMoving(p_owner, p_pattern.DamageProfile.Damage, p_pattern.DamageProfile.Impact,
                        pose.rotation * Vector3.forward, settings.MoveSpeed, settings.MaximumDistance))
                {
                    foreach (var created in attacks)
                    {
                        created.gameObject.SetActive(false);
                        Object.Destroy(created.gameObject);
                    }
                    return false;
                }
            }
            foreach (var attack in attacks)
                attack.gameObject.SetActive(true);
            return true;
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
