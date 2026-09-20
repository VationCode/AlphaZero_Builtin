using Alpha.AlphaCamera;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Range;
using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Player 원거리 발사와 근접 명중 값을 Local Camera Shake 표현으로 변환한다.
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponCameraShakeView : MonoBehaviour
    {
        private CombatModule _combatModule;
        private CameraCore _cameraCore;
        private RangeWeapon _rangeWeapon;
        private bool _isSubscribed;

        public void Bind(
            CombatModule p_combatModule,
            CameraCore p_cameraCore)
        {
            Unbind();

            if (p_combatModule == null ||
                p_cameraCore == null)
            {
                return;
            }

            _combatModule = p_combatModule;
            _cameraCore = p_cameraCore;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _combatModule = null;
            _cameraCore = null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _combatModule == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _combatModule.OnWeaponChanged += HandleWeaponChanged;
            _combatModule.OnMeleeSkillHitConfirmed +=
                HandleMeleeSkillHitConfirmed;
            _isSubscribed = true;

            BindRangeWeapon(_combatModule.CurrentRangeWeapon);
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && _combatModule != null)
            {
                _combatModule.OnWeaponChanged -= HandleWeaponChanged;
                _combatModule.OnMeleeSkillHitConfirmed -=
                    HandleMeleeSkillHitConfirmed;
            }

            _isSubscribed = false;
            BindRangeWeapon(null);
        }

        private void HandleWeaponChanged(WeaponDTO p_weapon)
        {
            BindRangeWeapon(_combatModule?.CurrentRangeWeapon);
        }

        private void BindRangeWeapon(RangeWeapon p_weapon)
        {
            if (_rangeWeapon == p_weapon)
                return;

            if (_rangeWeapon != null)
                _rangeWeapon.OnFired -= HandleRangeWeaponFired;

            _rangeWeapon = p_weapon;

            if (_rangeWeapon != null)
                _rangeWeapon.OnFired += HandleRangeWeaponFired;
        }

        private void HandleRangeWeaponFired(RangeAttackRequest p_request)
        {
            RequestShake(
                _rangeWeapon?.FireResponseSettings?.CameraShakeName);
        }

        private void HandleMeleeSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            RequestShake(p_skill?.CameraShakeName);
        }

        private void RequestShake(string p_name)
        {
            if (_cameraCore == null ||
                string.IsNullOrWhiteSpace(p_name))
            {
                return;
            }

            _cameraCore.RequestShake(p_name.Trim());
        }

    }
}
