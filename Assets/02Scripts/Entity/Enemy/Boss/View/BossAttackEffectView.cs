using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Boss
{
    // Flow가 알린 Rush 시점을 표현하고 생성한 이펙트의 수명과 취소 정리를 소유한다.
    [DisallowMultipleComponent]
    public sealed class BossAttackEffectView : MonoBehaviour
    {
        private BossCombatFlow _flow;
        private Transform _owner;
        private readonly List<GameObject> _instances = new();

        public void Bind(BossCombatFlow p_flow, Transform p_owner)
        {
            Unbind();
            _flow = p_flow;
            _owner = p_owner;
            if (_flow == null)
                return;
            _flow.OnRushEffectRequested += Play;
            _flow.OnAttackCancelled += Clear;
        }

        public void Unbind()
        {
            if (_flow != null)
            {
                _flow.OnRushEffectRequested -= Play;
                _flow.OnAttackCancelled -= Clear;
            }
            _flow = null;
            _owner = null;
            Clear();
        }

        private void Play(BossPatternData p_pattern, EBossRushEffectTiming p_timing)
        {
            if (!isActiveAndEnabled || _owner == null || p_pattern?.RushAttack == null)
                return;
            BossRushAttackSettings settings = p_pattern.RushAttack;
            for (int i = 0; i < settings.EffectCount; i++)
            {
                BossRushEffectSettings effect = settings.GetEffect(i);
                if (effect == null || !effect.IsValid || effect.Timing != p_timing)
                    continue;
                Transform point = effect.SpawnPoint != null ? effect.SpawnPoint : _owner;
                // Prefab 크기를 보존하며 Offset은 생성 지점의 회전 방향을 따른다.
                GameObject instance = Instantiate(effect.EffectPrefab,
                    point.position + point.rotation * effect.LocalOffset,
                    point.rotation * Quaternion.Euler(effect.LocalEulerAngles) * effect.EffectPrefab.transform.localRotation);
                if (effect.FollowSpawnPoint)
                    instance.transform.SetParent(point, true);
                _instances.Add(instance);
                instance.SetActive(true);
                foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
                    particle.Play();
                Destroy(instance, effect.Lifetime);
            }
        }

        private void Clear()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance == null)
                    continue;
                instance.SetActive(false);
                Destroy(instance);
            }
            _instances.Clear();
        }

        private void Update() => _instances.RemoveAll(p_instance => p_instance == null);
        private void OnDisable() => Clear();
        private void OnDestroy() => Unbind();
    }
}
