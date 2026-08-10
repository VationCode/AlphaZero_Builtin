using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 원거리 공격 성공 이벤트를 Muzzle과 Hitscan Tracer 효과로 표현한다.
    public sealed class RangeWeaponEffectView : MonoBehaviour
    {
        [SerializeField]
        private RangeWeapon _weapon;

        [SerializeField]
        private RangeWeaponAudioView _audioView;

        [SerializeField]
        private ParticleSystem _muzzleFlashPrefab;

        [SerializeField]
        private BulletTracerView _bulletTracerPrefab;

        [SerializeField, Min(0.01f)]
        private float _muzzleLifetime = 0.5f;

        [Header("Camera Shake")]
        [SerializeField]
        private CameraShakeSetting _cameraShakeSetting;

        private CameraCore _cameraCore;

        private void Awake()
        {
            _weapon ??= GetComponent<RangeWeapon>();
            _audioView ??= GetComponent<RangeWeaponAudioView>();
        }

        private void OnEnable()
        {
            if (_weapon != null)
                _weapon.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnFired -= HandleFired;
        }

        // Player가 장착한 무기에만 Local Camera 표현을 연결한다.
        public void BindCamera(
            CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void HandleFired(
            RangeAttackRequest p_request,
            RangeAttackResult p_result)
        {
            PlayMuzzle(p_request);
            _audioView?.PlayFire();

            _cameraCore?.RequestShake(
                _cameraShakeSetting);

            // 즉시 끝점이 결정되는 Hitscan만 별도 Tracer를 생성한다.
            if (!p_result.HasImmediateEndPoint ||
                _bulletTracerPrefab == null)
            {
                return;
            }

            BulletTracerView tracer =
                Instantiate(_bulletTracerPrefab);

            tracer.Play(
                p_request.Origin,
                p_result.EndPoint);
        }

        private void PlayMuzzle(
            in RangeAttackRequest p_request)
        {
            if (_muzzleFlashPrefab == null)
                return;

            ParticleSystem effect = Instantiate(
                _muzzleFlashPrefab,
                p_request.MuzzleOrigin,
                Quaternion.LookRotation(p_request.Direction));

            effect.Play(true);
            Destroy(effect.gameObject, _muzzleLifetime);
        }
    }
}
