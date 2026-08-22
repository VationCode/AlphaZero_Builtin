using Alpha.AlphaCamera;
using Alpha.Combat;
using Alpha.Item.Weapon.Melee;
using Alpha.Player.Combat;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Player의 확정 명중을 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class PlayerHitFeedbackView : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField]
        private string _hitShakeName = "Weak";

        private CombatModule _combatModule;
        private CameraCore _cameraCore;
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

            _combatModule.OnHitConfirmed += HandleHitConfirmed;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _combatModule == null)
                return;

            _combatModule.OnHitConfirmed -= HandleHitConfirmed;
            _isSubscribed = false;
        }

        private void HandleHitConfirmed(DamageInfo p_damageInfo)
        {
            // Melee는 콤보별 Shake를 직접 요청하므로 공통 Enemy Hit Shake를 중복하지 않는다.
            if (_combatModule?.CurrentWeapon is MeleeWeapon)
                return;

            _cameraCore?.RequestShake(
                _hitShakeName);
        }
    }
}
