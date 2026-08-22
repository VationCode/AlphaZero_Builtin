using System;
using Alpha.Combat;
using Alpha.Enemy;
using UnityEngine;

namespace Alpha.Enemy.Audio
{
    // 대표 Action State에 대응하는 SFX와 반복 재생 여부를 보관한다.
    [Serializable]
    public sealed class EnemyActionSfxSetting
    {
        [SerializeField]
        private EEnemyActionState _state;

        [SerializeField]
        private AudioClip[] _clips;

        [SerializeField]
        private bool _loop;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        public EEnemyActionState State => _state;
        public AudioClip[] Clips => _clips;
        public bool Loop => _loop;
        public float Volume => _volume;
    }

    // 공격 타입별 대기와 실제 실행 SFX를 각각 보관한다.
    [Serializable]
    public sealed class EnemyAttackSfxSetting
    {
        [SerializeField]
        private EEnemyAttackType _attackType;

        [SerializeField]
        private AudioClip[] _waitClips;

        [SerializeField]
        private AudioClip[] _attackClips;

        [SerializeField, Range(0f, 1f)]
        private float _waitVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _attackVolume = 1f;

        public EEnemyAttackType AttackType => _attackType;
        public AudioClip[] WaitClips => _waitClips;
        public AudioClip[] AttackClips => _attackClips;
        public float WaitVolume => _waitVolume;
        public float AttackVolume => _attackVolume;
    }

    // 피해 전달 방식별 Enemy 피격 SFX를 보관한다.
    [Serializable]
    public sealed class EnemyDamageSfxSetting
    {
        [SerializeField]
        private EDamageDeliveryType _deliveryType;

        [SerializeField]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        public EDamageDeliveryType DeliveryType => _deliveryType;
        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
    }

    // Audio 하위 객체에서 Enemy의 상태·공격·피격·사망 SFX를 표현한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyAudioView : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        [Header("Action State SFX")]
        [SerializeField]
        private EnemyActionSfxSetting[] _actionStateSfx;

        [Header("Attack SFX")]
        [SerializeField]
        private EnemyAttackSfxSetting[] _attackSfx;

        [Header("Damage SFX")]
        [SerializeField]
        private EnemyDamageSfxSetting[] _damageSfx;

        [SerializeField]
        private AudioClip[] _deathClips;

        [SerializeField, Range(0f, 1f)]
        private float _deathVolume = 1f;

        private EnemyActionFlow _actionFlow;
        private EnemyAttackFlow _attackFlow;
        private EnemyHealthModule _healthModule;
        private bool _isSubscribed;

        private void Awake()
        {
            _audioSource ??= GetComponent<AudioSource>();

            if (_audioSource != null)
                _audioSource.playOnAwake = false;
        }

        public void Bind(
            EnemyActionFlow p_actionFlow,
            EnemyAttackFlow p_attackFlow,
            EnemyHealthModule p_healthModule)
        {
            Unbind();

            _actionFlow = p_actionFlow;
            _attackFlow = p_attackFlow;
            _healthModule = p_healthModule;

            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _actionFlow = null;
            _attackFlow = null;
            _healthModule = null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopCurrentAudio();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                !isActiveAndEnabled ||
                _actionFlow == null ||
                _attackFlow == null ||
                _healthModule == null)
            {
                return;
            }

            _actionFlow.OnStateChanged += PlayActionState;
            _attackFlow.OnAttackWaitStarted += PlayAttackWait;
            _attackFlow.OnAttackStarted += PlayAttack;
            _healthModule.OnDamaged += HandleDamaged;
            _healthModule.OnDeath += PlayDeath;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            if (_actionFlow != null)
                _actionFlow.OnStateChanged -= PlayActionState;

            if (_attackFlow != null)
            {
                _attackFlow.OnAttackWaitStarted -= PlayAttackWait;
                _attackFlow.OnAttackStarted -= PlayAttack;
            }

            if (_healthModule != null)
            {
                _healthModule.OnDamaged -= HandleDamaged;
                _healthModule.OnDeath -= PlayDeath;
            }

            _isSubscribed = false;
        }

        // 상태 진입 시 이전 반복음을 정리하고 해당 상태의 SFX를 재생한다.
        public void PlayActionState(EEnemyActionState p_state)
        {
            StopLoop();

            EnemyActionSfxSetting setting =
                FindActionSetting(p_state);

            if (setting == null ||
                !TrySelectClip(
                    setting.Clips,
                    out AudioClip clip))
            {
                return;
            }

            if (!setting.Loop)
            {
                PlayOneShot(clip, setting.Volume);
                return;
            }

            if (_audioSource == null)
                return;

            _audioSource.clip = clip;
            _audioSource.loop = true;
            _audioSource.volume = setting.Volume;
            _audioSource.Play();
        }

        public void PlayAttackWait(EEnemyAttackType p_attackType)
        {
            EnemyAttackSfxSetting setting =
                FindAttackSetting(p_attackType);

            if (setting != null)
            {
                PlayRandomOneShot(
                    setting.WaitClips,
                    setting.WaitVolume);
            }
        }

        public void PlayAttack(EEnemyAttackType p_attackType)
        {
            EnemyAttackSfxSetting setting =
                FindAttackSetting(p_attackType);

            if (setting != null)
            {
                PlayRandomOneShot(
                    setting.AttackClips,
                    setting.AttackVolume);
            }
        }

        private void HandleDamaged(DamageInfo p_damageInfo)
        {
            EnemyDamageSfxSetting setting =
                FindDamageSetting(p_damageInfo.DeliveryType);

            if (setting != null)
            {
                PlayRandomOneShot(
                    setting.Clips,
                    setting.Volume);
            }
        }

        private void PlayDeath()
        {
            // 사망음이 이동 반복음과 겹치지 않도록 현재 재생을 먼저 종료한다.
            StopCurrentAudio();
            PlayRandomOneShot(
                _deathClips,
                _deathVolume);
        }

        private EnemyActionSfxSetting FindActionSetting(
            EEnemyActionState p_state)
        {
            if (_actionStateSfx == null)
                return null;

            foreach (EnemyActionSfxSetting setting in _actionStateSfx)
            {
                if (setting != null && setting.State == p_state)
                    return setting;
            }

            return null;
        }

        private EnemyAttackSfxSetting FindAttackSetting(
            EEnemyAttackType p_attackType)
        {
            if (_attackSfx == null)
                return null;

            foreach (EnemyAttackSfxSetting setting in _attackSfx)
            {
                if (setting != null &&
                    setting.AttackType == p_attackType)
                {
                    return setting;
                }
            }

            return null;
        }

        private EnemyDamageSfxSetting FindDamageSetting(
            EDamageDeliveryType p_deliveryType)
        {
            if (_damageSfx == null)
                return null;

            foreach (EnemyDamageSfxSetting setting in _damageSfx)
            {
                if (setting != null &&
                    setting.DeliveryType == p_deliveryType)
                {
                    return setting;
                }
            }

            // 매칭되지 않은 전달 방식은 다른 타격음으로 대체하지 않는다.
            return null;
        }

        private void PlayRandomOneShot(
            AudioClip[] p_clips,
            float p_volume)
        {
            if (TrySelectClip(
                    p_clips,
                    out AudioClip clip))
            {
                PlayOneShot(clip, p_volume);
            }
        }

        private void PlayOneShot(
            AudioClip p_clip,
            float p_volume)
        {
            if (_audioSource == null || p_clip == null)
                return;

            _audioSource.PlayOneShot(
                p_clip,
                Mathf.Clamp01(p_volume));
        }

        private static bool TrySelectClip(
            AudioClip[] p_clips,
            out AudioClip p_clip)
        {
            p_clip = null;

            if (p_clips == null || p_clips.Length == 0)
                return false;

            int startIndex = UnityEngine.Random.Range(
                0,
                p_clips.Length);

            for (int offset = 0; offset < p_clips.Length; offset++)
            {
                AudioClip clip = p_clips[
                    (startIndex + offset) % p_clips.Length];

                if (clip == null)
                    continue;

                p_clip = clip;
                return true;
            }

            return false;
        }

        private void StopLoop()
        {
            if (_audioSource == null || !_audioSource.loop)
                return;

            _audioSource.Stop();
            _audioSource.clip = null;
            _audioSource.loop = false;
            _audioSource.volume = 1f;
        }

        private void StopCurrentAudio()
        {
            if (_audioSource == null)
                return;

            _audioSource.Stop();
            _audioSource.clip = null;
            _audioSource.loop = false;
            _audioSource.volume = 1f;
        }
    }
}
