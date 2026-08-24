using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Combat
{
    // 공격 방식과 관계없이 모든 전투 Skill이 공유하는 표현 데이터를 보관한다.
    public abstract class CombatSkillDefinition : ScriptableObject
    {
        [Tooltip("외부 저장·로그에서 Skill을 구분하는 고유 ID입니다.")]
        [SerializeField]
        private string _skillId;

        [Tooltip("Animator 상태 이름입니다. 비어 있으면 Skill ID를 사용합니다.")]
        [SerializeField]
        private string _animationKey;

        [Tooltip("Skill 시작 시 무기 Audio View가 선택할 클립입니다.")]
        [SerializeField]
        private AudioClip[] _audioClips;

        public string SkillId => _skillId?.Trim();
        public string AnimationKey =>
            string.IsNullOrWhiteSpace(_animationKey)
                ? SkillId
                : _animationKey.Trim();
        public IReadOnlyList<AudioClip> AudioClips => _audioClips;

        public virtual bool IsValid =>
            !string.IsNullOrWhiteSpace(SkillId) &&
            !string.IsNullOrWhiteSpace(AnimationKey);

        protected virtual void OnValidate()
        {
            _skillId = _skillId?.Trim();
            _animationKey = _animationKey?.Trim();
            _audioClips ??= System.Array.Empty<AudioClip>();
        }
    }
}
