using Alpha.AlphaCamera;
using Alpha.Combat;
using Alpha.Player.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Effect
{
    // Player Melee Combo Name과 Camera Shake 설정 이름을 연결한다.
    [Serializable]
    public struct PlayerMeleeHitShakeBinding
    {
        [Tooltip("Player Melee Combo Settings에서 매칭할 Name")]
        [SerializeField]
        private string _comboName;

        [Tooltip("CameraShakeModule에 등록한 설정 이름")]
        [SerializeField]
        private string _shakeName;

        public string ComboName => _comboName;
        public string ShakeName => _shakeName;
    }

    // Player의 확정 명중을 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class PlayerHitFeedbackView : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField]
        private string _hitShakeName = "Weak";

        [Tooltip("Shake가 필요한 Melee 콤보만 Combo Name과 설정 이름으로 등록합니다.")]
        [SerializeField]
        private PlayerMeleeHitShakeBinding[] _meleeHitShakeBindings;

        private CombatModule _combatModule;
        private CameraCore _cameraCore;
        private bool _isSubscribed;
        private readonly Dictionary<string, string> _meleeHitShakeByComboName =
            new(StringComparer.Ordinal);

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
            _combatModule.OnMeleeHitConfirmed += HandleMeleeHitConfirmed;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _combatModule == null)
                return;

            _combatModule.OnHitConfirmed -= HandleHitConfirmed;
            _combatModule.OnMeleeHitConfirmed -= HandleMeleeHitConfirmed;
            _isSubscribed = false;
        }

        private void HandleHitConfirmed(DamageInfo p_damageInfo)
        {
            // Melee는 공격당 한 번 발생하는 콤보별 Shake 이벤트에서 처리한다.
            if (p_damageInfo.DeliveryType == EDamageDeliveryType.Melee)
                return;

            _cameraCore?.RequestShake(
                _hitShakeName);
        }

        private void HandleMeleeHitConfirmed(string p_comboName)
        {
            if (_cameraCore == null ||
                string.IsNullOrWhiteSpace(p_comboName) ||
                !_meleeHitShakeByComboName.TryGetValue(
                    p_comboName,
                    out string shakeName))
            {
                return;
            }

            _cameraCore.RequestShake(shakeName);
        }

        private void RebuildMeleeHitShakeMap(bool p_logWarnings)
        {
            _meleeHitShakeByComboName.Clear();

            if (_meleeHitShakeBindings == null)
                return;

            foreach (PlayerMeleeHitShakeBinding binding in
                     _meleeHitShakeBindings)
            {
                string comboName = binding.ComboName?.Trim();
                string shakeName = binding.ShakeName?.Trim();

                // 비어 있는 항목은 해당 콤보의 Shake를 생략한다.
                if (string.IsNullOrWhiteSpace(comboName) ||
                    string.IsNullOrWhiteSpace(shakeName))
                {
                    continue;
                }

                if (_meleeHitShakeByComboName.ContainsKey(comboName))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Player Melee Hit Shake의 Combo Name이 중복되었습니다: {comboName}",
                            this);
                    }

                    continue;
                }

                _meleeHitShakeByComboName.Add(comboName, shakeName);
            }
        }

        private void OnValidate()
        {
            RebuildMeleeHitShakeMap(false);
        }
    }
}
