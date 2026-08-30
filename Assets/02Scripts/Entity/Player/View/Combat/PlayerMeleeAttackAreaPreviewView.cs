using Alpha.Detection;
using Alpha.Item.Weapon.Melee;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Player.Combat
{
    // Player가 소유한 Skill별 공격 범위를 Scene에서 미리 보여준다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatModule))]
    [RequireComponent(typeof(DetectionAreaGizmoView))]
    public sealed class PlayerMeleeAttackAreaPreviewView : MonoBehaviour
    {
        [SerializeField]
        private CombatModule _combatModule;

        [SerializeField]
        private DetectionAreaGizmoView _areaGizmoView;

        [FormerlySerializedAs("_previewComboIndex")]
        [SerializeField, Min(0)]
        private int _previewSkillIndex;

        [Tooltip("Edit Mode에서 범위를 미리 볼 Melee Combo 자산입니다.")]
        [SerializeField]
        private MeleeComboDefinition _previewComboDefinition;

        [SerializeField]
        private bool _showPreview = true;

        [SerializeField]
        private Color _areaColor = new(1f, 0.65f, 0f, 1f);

        private void Reset()
        {
            _combatModule = GetComponent<CombatModule>();
            _areaGizmoView = GetComponent<DetectionAreaGizmoView>();
        }

        private void OnValidate()
        {
            _combatModule ??= GetComponent<CombatModule>();
            _areaGizmoView ??= GetComponent<DetectionAreaGizmoView>();
            _previewSkillIndex = Mathf.Max(0, _previewSkillIndex);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showPreview ||
                !TryResolveCombatModule() ||
                !TryResolveGizmoView())
            {
                return;
            }

            int skillIndex = Application.isPlaying &&
                             _combatModule.CurrentMeleeSkillIndex >= 0
                ? _combatModule.CurrentMeleeSkillIndex
                : _previewSkillIndex;

            MeleeSkillDefinition skill =
                _combatModule.GetMeleeSkillDefinition(skillIndex) ??
                _previewComboDefinition?.GetSkill(skillIndex);
            MeleeSkillAttackSettings settings = skill?.AttackSettings;

            if (settings == null || !settings.IsValid)
                return;

            Transform origin = ResolveOrigin();

            DetectionAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                origin,
                settings.Area);

            _areaGizmoView.Draw(request, _areaColor);
        }

        private bool TryResolveGizmoView()
        {
            _areaGizmoView ??= GetComponent<DetectionAreaGizmoView>();
            return _areaGizmoView != null;
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
