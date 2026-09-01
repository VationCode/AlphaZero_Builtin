using UnityEngine;

namespace Alpha.Enemy
{
    // Enemy의 공격 패턴 원본 설정만 보관하고 조회한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackPatternSettings : MonoBehaviour
    {
        [SerializeField]
        private EnemyAttackPatternSetting[] _patterns =
        {
            new()
        };

        public int Count => _patterns?.Length ?? 0;

        public EnemyAttackPatternSetting GetPattern(int p_index)
        {
            return p_index >= 0 && p_index < Count
                ? _patterns[p_index]
                : null;
        }

        // 기존 MonoBehaviour 배열 데이터를 새 설정 소유 구조로 이전한다.
        public void ReplacePatterns(EnemyAttackPatternSetting[] p_patterns)
        {
            _patterns = p_patterns;
            Validate();
        }

        public void Validate()
        {
            if (_patterns == null || _patterns.Length == 0)
            {
                _patterns = new[] { new EnemyAttackPatternSetting() };
            }

            for (int index = 0; index < _patterns.Length; index++)
            {
                _patterns[index] ??= new EnemyAttackPatternSetting();
                _patterns[index].Validate();
            }
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
