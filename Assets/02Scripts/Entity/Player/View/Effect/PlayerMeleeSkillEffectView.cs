using System.Collections.Generic;
using Alpha.Item.Weapon.Melee;
using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Melee Skill의 Effect 요청을 Player 자식의 실제 전투 Effect로 표현한다.
    public sealed class PlayerMeleeSkillEffectView : MonoBehaviour
    {
        private readonly List<GameObject> _activeEffects = new();
        private CombatModule _combatModule;
        private bool _isSubscribed;

        // 이 컴포넌트의 Transform을 모든 Melee Effect의 생성 부모로 사용한다.
        public void Bind(CombatModule p_combatModule)
        {
            if (ReferenceEquals(_combatModule, p_combatModule))
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _combatModule = p_combatModule;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _combatModule = null;
            StopAllEffects();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllEffects();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _combatModule == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _combatModule.OnMeleeSkillEffectRequested +=
                HandleEffectRequested;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _combatModule == null)
                return;

            _combatModule.OnMeleeSkillEffectRequested -=
                HandleEffectRequested;
            _isSubscribed = false;
        }

        private void HandleEffectRequested(MeleeSkillDefinition p_skill)
        {
            GameObject prefab = p_skill?.EffectPrefab;

            if (prefab == null)
                return;

            _activeEffects.RemoveAll(effect => effect == null);

            // Prefab의 Local Transform을 유지한 채 Player/Effect/Combat 자식으로 생성한다.
            GameObject effect = Instantiate(prefab, transform, false);
            ParticleSystem[] particles =
                effect.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (ParticleSystem particle in particles)
                particle.Play(true);

            _activeEffects.Add(effect);

            // 반복 Particle도 남은 Skill 시간 이후에는 반드시 정리한다.
            Destroy(effect, p_skill.EffectLifetime);
        }

        private void StopAllEffects()
        {
            foreach (GameObject effect in _activeEffects)
            {
                if (effect != null)
                    Destroy(effect);
            }

            _activeEffects.Clear();
        }
    }
}
