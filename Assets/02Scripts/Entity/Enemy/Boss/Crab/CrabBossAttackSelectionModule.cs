using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Enemy.Boss
{
    // 공격별 선택 조건을 보관하고 Scene 뷰에 선택 거리를 표시한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossAttackSelectionModule : MonoBehaviour
    {
        private const float MinimumDistanceGap = 0.01f;

        [Tooltip("직전에 완료한 공격을 다시 선택할 때 적용할 가중치 배율입니다.")]
        [SerializeField, Range(0.01f, 1f)] private float _previousAttackWeightMultiplier = 0.25f;

        [Header("Attack1 Selection")]
        [Tooltip("타깃이 이 거리보다 가까우면 Attack1을 선택하지 않습니다.")]
        [SerializeField, Min(0f)] private float _attack1MinSelectionDistance;
        [Tooltip("보스 중심에서 타깃 충돌체 표면까지의 수평 거리입니다. 이 거리에서 추적을 멈춥니다.")]
        [FormerlySerializedAs("_attack1MaxSelectionDistance")]
        [SerializeField, Min(0.01f)] private float _attack1StartDistance = 23f;
        [Tooltip("보스 정면에서 타깃까지 허용하는 최대 각도입니다.")]
        [SerializeField, Range(0f, 180f)] private float _attack1MaxSelectionAngle = 55f;
        [Tooltip("0이면 Attack1을 선택하지 않습니다.")]
        [SerializeField, Min(0f)] private float _attack1SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack1Cooldown;
        [SerializeField, Min(1)] private int _attack1MinRepeatCount = 1;
        [SerializeField, Min(1)] private int _attack1MaxRepeatCount = 1;
        [Tooltip("Attack1 선택 Min/Max 거리를 Scene 뷰에 360도 원으로 표시합니다.")]
        [FormerlySerializedAs("_showAttack1SelectionArea")]
        [SerializeField] private bool _showAttack1StartDistance = true;
        [FormerlySerializedAs("_attack1SelectionAreaColor")]
        [SerializeField] private Color _attack1StartDistanceColor = new(0.05f, 0.65f, 1f, 0.7f);

        [Header("Attack2 Selection")]
        [SerializeField, Min(0f)] private float _attack2MinSelectionDistance;
        [SerializeField, Min(0.01f)] private float _attack2MaxSelectionDistance = 17f;
        [SerializeField, Range(0f, 180f)] private float _attack2MaxSelectionAngle = 55f;
        [SerializeField, Min(0f)] private float _attack2SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack2Cooldown;
        [SerializeField, Min(1)] private int _attack2MinRepeatCount = 1;
        [SerializeField, Min(1)] private int _attack2MaxRepeatCount = 1;
        [SerializeField] private bool _showAttack2SelectionArea = true;
        [SerializeField] private Color _attack2SelectionColor = new(0.15f, 0.65f, 1f, 0.7f);

        [Header("Attack3 Selection")]
        [SerializeField, Min(0f)] private float _attack3MinSelectionDistance = 4f;
        [SerializeField, Min(0.01f)] private float _attack3MaxSelectionDistance = 20f;
        [SerializeField, Range(0f, 180f)] private float _attack3MaxSelectionAngle = 180f;
        [SerializeField, Min(0f)] private float _attack3SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack3Cooldown = 2f;
        [SerializeField, Min(1)] private int _attack3MinRepeatCount = 1;
        [SerializeField, Min(1)] private int _attack3MaxRepeatCount = 1;
        [SerializeField] private bool _showAttack3SelectionArea = true;
        [SerializeField] private Color _attack3SelectionColor = new(0.75f, 0.35f, 1f, 0.7f);

        [Header("Attack4 Selection")]
        [SerializeField, Min(0f)] private float _attack4MinSelectionDistance = 4f;
        [SerializeField, Min(0.01f)] private float _attack4MaxSelectionDistance = 20f;
        [SerializeField, Range(0f, 180f)] private float _attack4MaxSelectionAngle = 55f;
        [SerializeField, Min(0f)] private float _attack4SelectionWeight = 1f;
        [SerializeField, Min(0f)] private float _attack4Cooldown = 2f;
        [SerializeField, Min(1)] private int _attack4MinRepeatCount = 1;
        [SerializeField, Min(1)] private int _attack4MaxRepeatCount = 1;
        [SerializeField] private bool _showAttack4SelectionArea = true;
        [SerializeField] private Color _attack4SelectionColor = new(1f, 0.75f, 0.1f, 0.7f);

        private Transform _owner;
        private BossLocomotionModule _locomotion;

        internal float PreviousAttackWeightMultiplier => _previousAttackWeightMultiplier;

        internal AttackSelectionSettings Attack1 => new(
            _attack1MinSelectionDistance, _attack1StartDistance,
            _attack1MaxSelectionAngle, _attack1SelectionWeight, _attack1Cooldown,
            _attack1MinRepeatCount, _attack1MaxRepeatCount);

        internal AttackSelectionSettings Attack2 => new(
            _attack2MinSelectionDistance, _attack2MaxSelectionDistance,
            _attack2MaxSelectionAngle, _attack2SelectionWeight, _attack2Cooldown,
            _attack2MinRepeatCount, _attack2MaxRepeatCount);

        internal AttackSelectionSettings Attack3 => new(
            _attack3MinSelectionDistance, _attack3MaxSelectionDistance,
            _attack3MaxSelectionAngle, _attack3SelectionWeight, _attack3Cooldown,
            _attack3MinRepeatCount, _attack3MaxRepeatCount);

        internal AttackSelectionSettings Attack4 => new(
            _attack4MinSelectionDistance, _attack4MaxSelectionDistance,
            _attack4MaxSelectionAngle, _attack4SelectionWeight, _attack4Cooldown,
            _attack4MinRepeatCount, _attack4MaxRepeatCount);

        public void Bind(Transform owner, BossLocomotionModule locomotion)
        {
            _owner = owner;
            _locomotion = locomotion;
        }

        private void OnValidate()
        {
            _previousAttackWeightMultiplier = Mathf.Clamp(_previousAttackWeightMultiplier, 0.01f, 1f);
            ValidateSelection(ref _attack1MinSelectionDistance, ref _attack1StartDistance,
                ref _attack1MaxSelectionAngle, ref _attack1SelectionWeight,
                ref _attack1Cooldown, ref _attack1MinRepeatCount, ref _attack1MaxRepeatCount);
            ValidateSelection(ref _attack2MinSelectionDistance, ref _attack2MaxSelectionDistance,
                ref _attack2MaxSelectionAngle, ref _attack2SelectionWeight,
                ref _attack2Cooldown, ref _attack2MinRepeatCount, ref _attack2MaxRepeatCount);
            ValidateSelection(ref _attack3MinSelectionDistance, ref _attack3MaxSelectionDistance,
                ref _attack3MaxSelectionAngle, ref _attack3SelectionWeight,
                ref _attack3Cooldown, ref _attack3MinRepeatCount, ref _attack3MaxRepeatCount);
            ValidateSelection(ref _attack4MinSelectionDistance, ref _attack4MaxSelectionDistance,
                ref _attack4MaxSelectionAngle, ref _attack4SelectionWeight,
                ref _attack4Cooldown, ref _attack4MinRepeatCount, ref _attack4MaxRepeatCount);
        }

        private static void ValidateSelection(ref float minimumDistance, ref float maximumDistance,
            ref float maximumAngle, ref float weight, ref float cooldown,
            ref int minimumRepeats, ref int maximumRepeats)
        {
            minimumDistance = Mathf.Max(0f, minimumDistance);
            maximumDistance = Mathf.Max(minimumDistance + MinimumDistanceGap, maximumDistance);
            maximumAngle = Mathf.Clamp(maximumAngle, 0f, 180f);
            weight = Mathf.Max(0f, weight);
            cooldown = Mathf.Max(0f, cooldown);
            minimumRepeats = Mathf.Max(1, minimumRepeats);
            maximumRepeats = Mathf.Max(minimumRepeats, maximumRepeats);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView ||
                (!_showAttack1StartDistance && !_showAttack2SelectionArea &&
                 !_showAttack3SelectionArea && !_showAttack4SelectionArea))
                return;

            Transform owner = _owner != null ? _owner :
                GetComponentInParent<CrabBossCore>()?.transform;
            if (owner == null)
                return;

            Vector3 position = Application.isPlaying && _locomotion != null
                ? _locomotion.Position : owner.position;
            Vector3 forward = Vector3.ProjectOnPlane(owner.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            if (_showAttack1StartDistance)
                DrawSelectionAreaGizmo(position, forward, Attack1,
                    _attack1StartDistanceColor, "Attack1", 0);
            if (_showAttack2SelectionArea)
                DrawSelectionAreaGizmo(position, forward, Attack2,
                    _attack2SelectionColor, "Attack2", 1);
            if (_showAttack3SelectionArea)
                DrawSelectionAreaGizmo(position, forward, Attack3,
                    _attack3SelectionColor, "Attack3", 2);
            if (_showAttack4SelectionArea)
                DrawSelectionAreaGizmo(position, forward, Attack4,
                    _attack4SelectionColor, "Attack4", 3);
        }

        private static void DrawSelectionAreaGizmo(Vector3 position, Vector3 forward,
            AttackSelectionSettings settings, Color color, string label, int labelRow)
        {
            if (settings.MaxDistance <= 0f)
                return;

            Vector3 center = position + Vector3.up * 0.1f;
            Vector3 labelDirection = Quaternion.AngleAxis(labelRow * 90f,
                Vector3.up) * forward;
            float labelHeight = 0.3f + labelRow * 0.35f;
            Color previousColor = Handles.color;
            Handles.color = color;
            Handles.DrawWireDisc(center, Vector3.up, settings.MaxDistance);
            if (settings.MinDistance > 0f)
                Handles.DrawWireDisc(center, Vector3.up, settings.MinDistance);
            Handles.DrawLine(center + labelDirection * settings.MinDistance,
                center + labelDirection * settings.MaxDistance);
            Handles.Label(center + labelDirection * settings.MinDistance +
                Vector3.up * labelHeight,
                $"{label} pre-attack stop start / Min {settings.MinDistance:0.#}m");
            Handles.Label(center + labelDirection * settings.MaxDistance +
                Vector3.up * labelHeight,
                $"{label} pre-attack stop end / Max {settings.MaxDistance:0.#}m " +
                $"(facing ±{settings.MaxAngle:0.#}°)");
            Handles.color = previousColor;
        }
#endif
    }

    // Flow가 후보 판단에 사용하는 한 공격의 읽기 전용 설정 묶음이다.
    internal readonly struct AttackSelectionSettings
    {
        internal readonly float MinDistance;
        internal readonly float MaxDistance;
        internal readonly float MaxAngle;
        internal readonly float Weight;
        internal readonly float Cooldown;
        internal readonly int MinRepeats;
        internal readonly int MaxRepeats;

        internal AttackSelectionSettings(float minDistance, float maxDistance,
            float maxAngle, float weight, float cooldown,
            int minRepeats, int maxRepeats)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            MaxAngle = maxAngle;
            Weight = weight;
            Cooldown = cooldown;
            MinRepeats = minRepeats;
            MaxRepeats = maxRepeats;
        }
    }
}
