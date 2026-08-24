using Alpha.AlphaCamera;
using Alpha.Combat;
using Alpha.Player.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Player Melee Skill 자산과 Camera Shake 설정 이름을 연결한다.
    [Serializable]
    public struct PlayerMeleeHitShakeBinding
    {
        [Tooltip("Camera Shake를 적용할 공통 Skill 자산")]
        [SerializeField]
        private CombatSkillDefinition _skill;

        [Tooltip("CameraShakeModule에 등록한 설정 이름")]
        [SerializeField]
        private string _shakeName;

        public CombatSkillDefinition Skill => _skill;
        public string ShakeName => _shakeName;
    }

    // Player의 확정 명중을 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class PlayerTargetHitFeedbackView : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField]
        private string _hitShakeName = "Weak";

        [Tooltip("Shake가 필요한 Melee Skill만 Skill ID와 설정 이름으로 등록합니다.")]
        [SerializeField]
        private PlayerMeleeHitShakeBinding[] _meleeHitShakeBindings;

        private CombatModule _combatModule;
        private CameraCore _cameraCore;
        private bool _isSubscribed;
        private readonly Dictionary<CombatSkillDefinition, string>
            _meleeHitShakeBySkill = new();

        private void Awake()
        {
            RebuildMeleeHitShakeMap(true);
        }

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
            _combatModule.OnMeleeSkillHitConfirmed +=
                HandleMeleeSkillHitConfirmed;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _combatModule == null)
                return;

            _combatModule.OnHitConfirmed -= HandleHitConfirmed;
            _combatModule.OnMeleeSkillHitConfirmed -=
                HandleMeleeSkillHitConfirmed;
            _isSubscribed = false;
        }

        private void HandleHitConfirmed(DamageInfo p_damageInfo)
        {
            // Melee는 공격당 한 번 발생하는 Skill별 Shake 이벤트에서 처리한다.
            if (p_damageInfo.DeliveryType == EDamageDeliveryType.Melee)
                return;

            _cameraCore?.RequestShake(
                _hitShakeName);
        }

        private void HandleMeleeSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            if (_cameraCore == null ||
                p_skill == null ||
                !_meleeHitShakeBySkill.TryGetValue(
                    p_skill,
                    out string shakeName))
            {
                return;
            }

            _cameraCore.RequestShake(shakeName);
        }

        private void RebuildMeleeHitShakeMap(bool p_logWarnings)
        {
            _meleeHitShakeBySkill.Clear();

            if (_meleeHitShakeBindings == null)
                return;

            foreach (PlayerMeleeHitShakeBinding binding in
                     _meleeHitShakeBindings)
            {
                CombatSkillDefinition skill = binding.Skill;
                string shakeName = binding.ShakeName?.Trim();

                // 비어 있는 항목은 해당 Skill의 Shake를 생략한다.
                if (skill == null ||
                    string.IsNullOrWhiteSpace(shakeName))
                {
                    continue;
                }

                if (_meleeHitShakeBySkill.ContainsKey(skill))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Player Melee Hit Shake의 Skill 자산이 중복되었습니다: {skill.name}",
                            this);
                    }

                    continue;
                }

                _meleeHitShakeBySkill.Add(skill, shakeName);
            }
        }

        private void OnValidate()
        {
            RebuildMeleeHitShakeMap(false);
        }
    }
}
