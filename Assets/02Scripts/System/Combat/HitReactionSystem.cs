using System;
using UnityEngine;

namespace Alpha.Combat
{
    // 공용 설정 타입의 수치와 조회·반응 요청을 한곳에서 관리한다.
    // Resources/Combat/HitReactionSystem 에 둔 단일 에셋을 모든 공격이 사용한다.
    public sealed class HitReactionSystem : ScriptableObject
    {
        public const string ResourcePath = "Combat/HitReactionSystem";

        [Serializable]
        public sealed class ReactionSetting
        {
            [Tooltip("공격이 참조하는 고유 타입 이름입니다. 사용 중인 이름은 유지하세요.")]
            [SerializeField] private string _type;
            [Tooltip("None은 상태 전이 없이 넉백만 적용합니다.")]
            [SerializeField] private EHitType _hitType;
            [SerializeField, Min(0f)] private float _knockbackDistance;
            [SerializeField, Min(0f)] private float _knockbackDuration;
            [Tooltip("피격 유지 시간입니다. Knockdown/Launch는 누워 있는 시간입니다.")]
            [SerializeField, Min(0f)] private float _recoveryDuration;

            public string Type => _type;
            public AttackImpactInfo CreateInfo() => new(
                _hitType, _knockbackDistance, _knockbackDuration, _recoveryDuration);
        }

        [SerializeField] private ReactionSetting[] _types = Array.Empty<ReactionSetting>();
        public int TypeCount => _types?.Length ?? 0;
        public string GetTypeName(int p_index) => _types[p_index]?.Type;

        private static HitReactionSystem _shared;
        public static HitReactionSystem Shared =>
            _shared != null ? _shared : _shared = Resources.Load<HitReactionSystem>(ResourcePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetShared() => _shared = null;

        public bool TryGetImpact(string p_type, out AttackImpactInfo p_impact)
        {
            p_impact = default;
            if (string.IsNullOrWhiteSpace(p_type) || _types == null)
                return false;

            ReactionSetting found = null;
            foreach (ReactionSetting setting in _types)
            {
                if (setting == null || !string.Equals(setting.Type, p_type, StringComparison.Ordinal))
                    continue;
                // 중복 타입은 임의로 하나를 적용하지 않는다.
                if (found != null)
                    return false;
                found = setting;
            }

            if (found == null)
                return false;
            p_impact = found.CreateInfo();
            return true;
        }

        // 피해 없이 넉백·넉다운만 요청할 때 사용한다. 실제 실행 여부는 대상이 결정한다.
        public static bool TryApply(
            Component p_target, string p_type, Transform p_attacker, Vector3 p_direction)
        {
            if (p_target == null || p_attacker == null || Shared == null ||
                !Shared.TryGetImpact(p_type, out AttackImpactInfo impact))
                return false;

            if (p_target.transform == p_attacker || p_target.transform.IsChildOf(p_attacker))
                return false;

            IHitReactionReceiver receiver = p_target.GetComponentInParent<IHitReactionReceiver>();
            if (receiver == null)
            {
                // 같은 Scene 부모 아래 있는 다른 Entity의 반응을 호출하지 않게 경계를 좁힌다.
                Rigidbody body = p_target.GetComponentInParent<Rigidbody>();
                CharacterController controller = p_target.GetComponentInParent<CharacterController>();
                Transform root = body != null ? body.transform :
                    controller != null ? controller.transform : p_target.transform;
                receiver = root.GetComponentInChildren<IHitReactionReceiver>(true);
            }
            return receiver != null && receiver.TryApplyHitReaction(impact, p_attacker, p_direction);
        }

        private void OnValidate()
        {
            if (_types == null)
                return;
            var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (ReactionSetting setting in _types)
            {
                if (setting == null || string.IsNullOrWhiteSpace(setting.Type) || !names.Add(setting.Type))
                    Debug.LogError("HitReactionSystem: 비어 있거나 중복된 설정 타입이 있습니다.", this);
            }
        }
    }
}
