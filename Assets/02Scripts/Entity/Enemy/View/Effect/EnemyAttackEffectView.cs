using System;
using System.Collections.Generic;
using Alpha.Enemy.Animation;
using UnityEngine;

namespace Alpha.Enemy.Effect
{
    // 공격 애니메이션 경과 시간에 맞춰 Enemy의 전투 Effect만 표현한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackEffectView : MonoBehaviour
    {
        [Tooltip("Attack Type과 Animation Index별 Effect 재생 구간입니다.")]
        [SerializeField]
        private EnemyAttackEffectTrack[] _attackEffects =
            Array.Empty<EnemyAttackEffectTrack>();

        private readonly List<GameObject> _spawnedEffects = new();

        private EnemyAnimationView _animationView;
        private EnemyCombatFlow _combatFlow;
        private EnemyAttackEffectTrack _currentTrack;
        private bool[] _startedTimings = Array.Empty<bool>();
        private bool[] _stoppedTimings = Array.Empty<bool>();
        private GameObject[] _currentInstances = Array.Empty<GameObject>();
        private bool _isAttackActive;
        private bool _isSubscribed;

        public void Bind(
            EnemyAnimationView p_animationView,
            EnemyCombatFlow p_combatFlow)
        {
            Unsubscribe();
            StopCurrentEffects(true);

            _animationView = p_animationView;
            _combatFlow = p_combatFlow;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            StopCurrentEffects(true);
            StopAllSpawnedEffects();
            _animationView = null;
            _combatFlow = null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopCurrentEffects(true);
            StopAllSpawnedEffects();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                !isActiveAndEnabled ||
                _animationView == null ||
                _combatFlow == null)
            {
                return;
            }

            _combatFlow.OnAttackStarted += HandleAttackStarted;
            _combatFlow.OnStateChanged += HandleCombatStateChanged;
            _animationView.OnAttackAnimationElapsed +=
                HandleAttackAnimationElapsed;
            _animationView.OnAttackAnimationCompleted +=
                HandleAttackAnimationCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            if (_combatFlow != null)
            {
                _combatFlow.OnAttackStarted -= HandleAttackStarted;
                _combatFlow.OnStateChanged -= HandleCombatStateChanged;
            }

            if (_animationView != null)
            {
                _animationView.OnAttackAnimationElapsed -=
                    HandleAttackAnimationElapsed;
                _animationView.OnAttackAnimationCompleted -=
                    HandleAttackAnimationCompleted;
            }

            _isSubscribed = false;
        }

        private void HandleAttackStarted(
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            StopCurrentEffects(true);
            RemoveDestroyedEffects();

            _currentTrack = FindTrack(
                p_attackType,
                p_animationIndex);

            int timingCount = _currentTrack?.TimingCount ?? 0;
            _startedTimings = new bool[timingCount];
            _stoppedTimings = new bool[timingCount];
            _currentInstances = new GameObject[timingCount];
            _isAttackActive = _currentTrack != null;
        }

        // 한 Frame에 여러 경계 시간을 지나도 시작과 종료를 순서대로 빠짐없이 처리한다.
        private void HandleAttackAnimationElapsed(
            float p_elapsedSeconds,
            float p_durationSeconds)
        {
            if (!_isAttackActive || _currentTrack == null)
                return;

            float elapsedSeconds = Mathf.Max(0f, p_elapsedSeconds);

            for (int index = 0;
                 index < _currentTrack.TimingCount;
                 index++)
            {
                EnemyAttackEffectTimingSetting timing =
                    _currentTrack.GetTiming(index);

                if (timing == null || !timing.IsValid)
                    continue;

                if (!_startedTimings[index] &&
                    elapsedSeconds >= timing.StartTimeSeconds)
                {
                    _startedTimings[index] = true;
                    _currentInstances[index] = PlayEffect(timing);
                }

                if (!_startedTimings[index] ||
                    _stoppedTimings[index] ||
                    elapsedSeconds < timing.EndTimeSeconds)
                {
                    continue;
                }

                _stoppedTimings[index] = true;
                StopEffect(
                    _currentInstances[index],
                    timing.TailDuration,
                    false);
                _currentInstances[index] = null;
            }
        }

        private void HandleAttackAnimationCompleted()
        {
            StopCurrentEffects(false);
        }

        private void HandleCombatStateChanged(
            EEnemyCombatState p_state)
        {
            if (!_isAttackActive ||
                p_state == EEnemyCombatState.Attack)
            {
                return;
            }

            // CombatFlow가 AnimationCompleted보다 먼저 Wait로 바뀌어도 Particle 잔상을 유지한다.
            bool clearImmediately =
                p_state != EEnemyCombatState.Wait;

            StopCurrentEffects(clearImmediately);

            if (clearImmediately)
                StopAllSpawnedEffects();
        }

        private EnemyAttackEffectTrack FindTrack(
            EEnemyAttackType p_attackType,
            int p_animationIndex)
        {
            EnemyAttackEffectTrack fallback = null;

            for (int index = 0;
                 index < (_attackEffects?.Length ?? 0);
                 index++)
            {
                EnemyAttackEffectTrack track = _attackEffects[index];

                if (track == null || track.AttackType != p_attackType)
                    continue;

                if (track.AnimationIndex == p_animationIndex)
                    return track;

                if (track.AnimationIndex == -1)
                    fallback ??= track;
            }

            return fallback;
        }

        private GameObject PlayEffect(
            EnemyAttackEffectTimingSetting p_timing)
        {
            Transform spawnPoint = p_timing.SpawnPoint != null
                ? p_timing.SpawnPoint
                : transform;
            GameObject instance = Instantiate(
                p_timing.EffectPrefab,
                spawnPoint,
                false);

            if (!p_timing.FollowSpawnPoint)
                instance.transform.SetParent(null, true);

            instance.SetActive(true);

            ParticleSystem[] particles =
                instance.GetComponentsInChildren<ParticleSystem>(true);

            for (int index = 0; index < particles.Length; index++)
            {
                particles[index].Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            for (int index = 0; index < particles.Length; index++)
                particles[index].Play(true);

            _spawnedEffects.Add(instance);
            return instance;
        }

        private void StopCurrentEffects(bool p_clearImmediately)
        {
            for (int index = 0;
                 index < _currentInstances.Length;
                 index++)
            {
                GameObject instance = _currentInstances[index];

                if (instance == null)
                    continue;

                EnemyAttackEffectTimingSetting timing =
                    _currentTrack?.GetTiming(index);

                StopEffect(
                    instance,
                    timing?.TailDuration ?? 0f,
                    p_clearImmediately);
            }

            _currentTrack = null;
            _startedTimings = Array.Empty<bool>();
            _stoppedTimings = Array.Empty<bool>();
            _currentInstances = Array.Empty<GameObject>();
            _isAttackActive = false;
        }

        private static void StopEffect(
            GameObject p_instance,
            float p_tailDuration,
            bool p_clearImmediately)
        {
            if (p_instance == null)
                return;

            ParticleSystem[] particles =
                p_instance.GetComponentsInChildren<ParticleSystem>(true);

            for (int index = 0; index < particles.Length; index++)
            {
                particles[index].Stop(
                    true,
                    p_clearImmediately
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting);
            }

            if (p_clearImmediately || particles.Length == 0)
            {
                p_instance.SetActive(false);
                Destroy(p_instance);
                return;
            }

            Destroy(p_instance, Mathf.Max(0f, p_tailDuration));
        }

        private void StopAllSpawnedEffects()
        {
            for (int index = 0;
                 index < _spawnedEffects.Count;
                 index++)
            {
                StopEffect(_spawnedEffects[index], 0f, true);
            }

            _spawnedEffects.Clear();
        }

        private void RemoveDestroyedEffects()
        {
            _spawnedEffects.RemoveAll(p_effect => p_effect == null);
        }

        private void OnValidate()
        {
            _attackEffects ??= Array.Empty<EnemyAttackEffectTrack>();

            for (int index = 0;
                 index < _attackEffects.Length;
                 index++)
            {
                _attackEffects[index] ??= new EnemyAttackEffectTrack();
                _attackEffects[index].Validate();
            }
        }
    }
}
