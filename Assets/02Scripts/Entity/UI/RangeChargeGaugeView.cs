using Alpha.Item.Weapon.Range;
using Alpha.Player.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.UI
{
    // 활성 원거리 무기의 차징 비율을 Filled Image로 표현한다.
    [DisallowMultipleComponent]
    public sealed class RangeChargeGaugeView : MonoBehaviour
    {
        [SerializeField]
        private Image _fillImage;

        private CombatModule _combatModule;

        private void Awake()
        {
            ResetGauge();
        }

        public bool Bind(CombatModule p_combatModule)
        {
            Unbind();

            if (p_combatModule == null || _fillImage == null)
                return false;

            _combatModule = p_combatModule;
            return true;
        }

        // Combat Flow 갱신 후 최신 차징 비율만 UI에 반영한다.
        private void LateUpdate()
        {
            RangeWeapon rangeWeapon =
                _combatModule?.ActiveRangeSecondaryWeapon;

            bool isCharging =
                rangeWeapon != null &&
                rangeWeapon.IsChargeEnabled &&
                rangeWeapon.IsSecondaryActive;

            _fillImage.fillAmount =
                isCharging ? rangeWeapon.ChargeRatio : 0f;
        }

        public void Unbind()
        {
            _combatModule = null;
            ResetGauge();
        }

        private void ResetGauge()
        {
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
        }

        private void OnDisable()
        {
            ResetGauge();
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
