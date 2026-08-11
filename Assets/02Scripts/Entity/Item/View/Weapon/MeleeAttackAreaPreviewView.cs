using Alpha.Combat;
using Alpha.Item.Weapon.Melee;
using UnityEngine;

namespace Alpha.Item.View.Weapon
{
    // 근접 무기의 콤보별 AttackAreaSettings를 Scene Gizmo로 미리 보여준다.
    [DisallowMultipleComponent]
    public sealed class MeleeAttackAreaPreviewView : MonoBehaviour
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [Tooltip("편집 모드에서 공격 방향의 기준으로 사용할 Transform")]
        [SerializeField]
        private Transform _origin;

        [SerializeField, Min(0)]
        private int _previewComboIndex;

        [SerializeField]
        private bool _showPreview = true;

        [SerializeField]
        private Color _areaColor = new(1f, 0.65f, 0f, 1f);

        private void Reset()
        {
            _weapon = GetComponent<MeleeWeapon>();
            _origin = transform;
        }

        private void OnValidate()
        {
            _weapon ??= GetComponent<MeleeWeapon>();
            _origin ??= transform;
            _previewComboIndex = Mathf.Max(0, _previewComboIndex);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showPreview)
                return;

            _weapon ??= GetComponent<MeleeWeapon>();

            if (_weapon == null)
                return;

            int comboIndex = Application.isPlaying &&
                             _weapon.CurrentComboIndex >= 0
                ? _weapon.CurrentComboIndex
                : _previewComboIndex;

            MeleeAttackSettings settings =
                _weapon.GetAttackSettings(comboIndex);

            if (settings == null || !settings.IsValid)
                return;

            Transform origin = Application.isPlaying &&
                               _weapon.AttackSource != null
                ? _weapon.AttackSource
                : _origin != null
                    ? _origin
                    : transform;

            AttackAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                origin,
                settings.Area);

            AttackAreaGizmoDrawer.Draw(request, _areaColor);
        }
    }
}
