using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Melee
{
    // 선택된 Skill 하나의 시간과 타격·Effect 실행 시점만 관리한다.
    internal sealed class MeleeWeaponSkillFlow
    {
        public MeleeSkillDefinition CurrentSkill { get; private set; }
        public float ElapsedTime { get; private set; }

        private bool _didExecuteHit;
        private bool _didRequestEffect;

        public bool TryBegin(MeleeSkillDefinition p_skill)
        {
            if (p_skill == null || !p_skill.IsValid)
                return false;

            CurrentSkill = p_skill;
            ElapsedTime = 0f;
            _didExecuteHit = false;
            _didRequestEffect = false;
            return true;
        }

        public void Tick(
            float p_deltaTime,
            MeleeWeaponAttackModule p_attackModule,
            Action<MeleeSkillDefinition> p_effectRequested)
        {
            if (CurrentSkill == null)
                return;

            ElapsedTime += Mathf.Max(0f, p_deltaTime);
            TryExecuteHit(p_attackModule);
            TryRequestEffect(p_effectRequested);
        }

        // 시작 시간이 0인 Effect도 Skill 시작 프레임에 요청할 수 있다.
        public void TryRequestEffect(
            Action<MeleeSkillDefinition> p_effectRequested)
        {
            if (_didRequestEffect ||
                CurrentSkill == null ||
                ElapsedTime < CurrentSkill.EffectStartTime)
            {
                return;
            }

            _didRequestEffect = true;
            p_effectRequested?.Invoke(CurrentSkill);
        }

        public void End()
        {
            CurrentSkill = null;
            ElapsedTime = 0f;
            _didExecuteHit = false;
            _didRequestEffect = false;
        }

        private void TryExecuteHit(
            MeleeWeaponAttackModule p_attackModule)
        {
            MeleeSkillAttackSettings settings =
                CurrentSkill?.AttackSettings;

            if (_didExecuteHit ||
                settings == null ||
                !settings.IsValid ||
                ElapsedTime < settings.HitTime)
            {
                return;
            }

            _didExecuteHit = true;
            p_attackModule?.Execute(CurrentSkill);
        }
    }
}
