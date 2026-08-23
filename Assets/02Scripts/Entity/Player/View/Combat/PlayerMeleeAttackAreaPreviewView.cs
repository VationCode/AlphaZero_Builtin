using Alpha.Detection;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player가 소유한 콤보별 공격 범위를 Scene에서 미리 보여준다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatModule))]
    public sealed class PlayerMeleeAttackAreaPreviewView : MonoBehaviour
    {
        [SerializeField]
        private CombatModule _combatModule;

        [SerializeField, Min(0)]
        private int _previewComboIndex;

        [SerializeField]
        private bool _showPreview = true;

        [SerializeField]
        private Color _areaColor = new(1f, 0.65f, 0f, 1f);

        private void Reset()
        {
            _combatModule = GetComponent<CombatModule>();
        }

        private void OnValidate()
        {
            _combatModule ??= GetComponent<CombatModule>();
            _previewComboIndex = Mathf.Max(0, _previewComboIndex);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showPreview || !TryResolveCombatModule())
                return;

            int comboIndex = Application.isPlaying &&
                             _combatModule.CurrentMeleeComboIndex >= 0
                ? _combatModule.CurrentMeleeComboIndex
                : _previewComboIndex;

            PlayerMeleeAttackSettings settings =
                _combatModule.GetMeleeAttackSettings(comboIndex);

            if (settings == null || !settings.IsValid)
                return;

            Transform origin = ResolveOrigin();

            DetectionAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                origin,
                settings.Area);

            DetectionAreaGizmoDrawer.Draw(request, _areaColor);
        }

        private bool TryResolveCombatModule()
        {
            _combatModule ??= GetComponent<CombatModule>();

            if (_combatModule != null)
                return true;

            PlayerCore playerCore = GetComponentInParent<PlayerCore>();
            _combatModule = playerCore?.CombatModule;
            return _combatModule != null;
        }

        private Transform ResolveOrigin()
        {
            if (Application.isPlaying &&
                _combatModule.MeleeAttackSource != null)
            {
                return _combatModule.MeleeAttackSource;
            }

            PlayerCore playerCore = GetComponentInParent<PlayerCore>();
            return playerCore != null
                ? playerCore.transform
                : transform;
        }
    }
}
