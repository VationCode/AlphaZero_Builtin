using Alpha.AlphaCamera;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
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
        private MeleeSkillDefinition _skill;

        [Tooltip("CameraShakeModule에 등록한 설정 이름")]
        [SerializeField]
        private string _shakeName;

        public MeleeSkillDefinition Skill => _skill;
        public string ShakeName => _shakeName;
    }

    // Player 원거리 발사와 근접 명중 값을 Local Camera Shake 표현으로 변환한다.
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponCameraShakeView : MonoBehaviour
    {
        [Tooltip("Shake가 필요한 Melee Skill만 Skill 자산과 설정 이름으로 등록합니다.")]
        [SerializeField]
        private PlayerMeleeHitShakeBinding[] _meleeHitShakeBindings;

        private CombatModule _combatModule;
        private CameraCore _cameraCore;
        private RangeWeapon _rangeWeapon;
        private bool _isSubscribed;

        private readonly Dictionary<MeleeSkillDefinition, string>
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

            _combatModule.OnWeaponChanged += HandleWeaponChanged;
            _combatModule.OnMeleeSkillHitConfirmed +=
                HandleMeleeSkillHitConfirmed;
            _isSubscribed = true;

            BindRangeWeapon(_combatModule.CurrentRangeWeapon);
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && _combatModule != null)
            {
                _combatModule.OnWeaponChanged -= HandleWeaponChanged;
                _combatModule.OnMeleeSkillHitConfirmed -=
                    HandleMeleeSkillHitConfirmed;
            }

            _isSubscribed = false;
            BindRangeWeapon(null);
        }

        private void HandleWeaponChanged(WeaponDTO p_weapon)
        {
            BindRangeWeapon(_combatModule?.CurrentRangeWeapon);
        }

        private void BindRangeWeapon(RangeWeapon p_weapon)
        {
            if (_rangeWeapon == p_weapon)
                return;

            if (_rangeWeapon != null)
                _rangeWeapon.OnFired -= HandleRangeWeaponFired;

            _rangeWeapon = p_weapon;

            if (_rangeWeapon != null)
                _rangeWeapon.OnFired += HandleRangeWeaponFired;
        }

        private void HandleRangeWeaponFired(RangeAttackRequest p_request)
        {
            RequestShake(
                _rangeWeapon?.FireResponseSettings?.CameraShakeName);
        }

        private void HandleMeleeSkillHitConfirmed(
            MeleeSkillDefinition p_skill)
        {
            if (p_skill == null ||
                !_meleeHitShakeBySkill.TryGetValue(
                    p_skill,
                    out string shakeName))
            {
                return;
            }

            RequestShake(shakeName);
        }

        private void RequestShake(string p_name)
        {
            if (_cameraCore == null ||
                string.IsNullOrWhiteSpace(p_name))
            {
                return;
            }

            _cameraCore.RequestShake(p_name.Trim());
        }

        private void RebuildMeleeHitShakeMap(bool p_logWarnings)
        {
            _meleeHitShakeBySkill.Clear();

            if (_meleeHitShakeBindings == null)
                return;

            foreach (PlayerMeleeHitShakeBinding binding in
                     _meleeHitShakeBindings)
            {
                MeleeSkillDefinition skill = binding.Skill;
                string shakeName = binding.ShakeName?.Trim();

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
