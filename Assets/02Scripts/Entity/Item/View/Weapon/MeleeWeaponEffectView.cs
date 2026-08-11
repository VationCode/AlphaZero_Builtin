using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Melee;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 근접 공격의 실제 적중을 콤보별 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponEffectView : MonoBehaviour
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [Tooltip("Combo Clips와 같은 인덱스의 적중 Camera Shake 설정")]
        [SerializeField]
        private CameraShakeSetting[] _hitShakeSettings;

        private CameraCore _cameraCore;

        private void Awake()
        {
            _weapon ??= GetComponent<MeleeWeapon>();
        }

        private void OnEnable()
        {
            if (_weapon != null)
                _weapon.OnHitConfirmed += HandleHitConfirmed;
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnHitConfirmed -= HandleHitConfirmed;

            _cameraCore = null;
        }

        // Player가 실제로 장착한 무기에만 Local Camera를 연결한다.
        public void BindCamera(CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void HandleHitConfirmed(int p_comboIndex)
        {
            if (_cameraCore == null ||
                _hitShakeSettings == null ||
                p_comboIndex < 0 ||
                p_comboIndex >= _hitShakeSettings.Length)
            {
                return;
            }

            CameraShakeSetting setting =
                _hitShakeSettings[p_comboIndex];

            if (setting.IsValid)
                _cameraCore.RequestShake(setting);
        }
    }
}
