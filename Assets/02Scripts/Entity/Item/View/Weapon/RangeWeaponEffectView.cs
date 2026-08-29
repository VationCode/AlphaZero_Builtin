using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 원거리 공격 성공 이벤트를 Muzzle·Audio·Camera Shake로 표현한다.
    public sealed class RangeWeaponEffectView : MonoBehaviour
    {
        [SerializeField]
        private RangeAttackModule _attackModule;

        [SerializeField]
        private RangeWeaponAudioView _audioView;

        [SerializeField]
        private ParticleSystem _muzzleFlashPrefab;

        [SerializeField, Min(0.01f)]
        private float _muzzleLifetime = 0.5f;

        [Header("Camera Shake")]
        [SerializeField]
        private string _fireShakeName = "Weak";

        private CameraCore _cameraCore;

        private void Awake()
        {
            _attackModule ??= GetComponent<RangeAttackModule>();
            _audioView ??= GetComponent<RangeWeaponAudioView>();
        }

        private void OnEnable()
        {
            if (_attackModule != null)
                _attackModule.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            if (_attackModule != null)
                _attackModule.OnFired -= HandleFired;
        }

        // Player가 장착한 무기에만 Local Camera 표현을 연결한다.
        public void BindCamera(
            CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void HandleFired(RangeAttackRequest p_request)
        {
            PlayMuzzle(p_request);
            _audioView?.PlayFire();

            _cameraCore?.RequestShake(
                _fireShakeName);
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
