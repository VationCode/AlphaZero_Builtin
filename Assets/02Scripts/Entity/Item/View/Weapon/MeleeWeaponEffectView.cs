using Alpha.AlphaCamera;
using Alpha.Item.Weapon.Melee;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // 콤보 인덱스와 Camera Shake preset을 배열 순서와 무관하게 연결한다.
    [Serializable]
    public struct MeleeHitShakeBinding
    {
        [Tooltip("MeleeWeapon Combo Clips에서 매칭할 콤보 인덱스")]
        [SerializeField, Min(0)]
        private int _comboIndex;

        [Tooltip("CameraShakeModule에 등록한 preset 이름")]
        [SerializeField]
        private string _shakeName;

        public int ComboIndex => _comboIndex;
        public string ShakeName => _shakeName;
    }

    // 근접 공격의 실제 적중을 콤보별 Local Camera Shake로 표현한다.
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponEffectView : MonoBehaviour
    {
        [SerializeField]
        private MeleeWeapon _weapon;

        [Tooltip("Shake가 필요한 콤보만 Combo Index와 preset 이름으로 등록합니다.")]
        [SerializeField]
        private MeleeHitShakeBinding[] _hitShakeBindings;

        private CameraCore _cameraCore;
        private readonly Dictionary<int, string> _hitShakeByComboIndex = new();

        private void Awake()
        {
            _weapon ??= GetComponent<MeleeWeapon>();
            RebuildHitShakeMap(true);
        }

        private void OnEnable()
        {
            if (_weapon != null)
                _weapon.OnHitConfirmed += HandleHitConfirmed;
        }

        private void OnDisable()
        {
            if (_weapon != null)
                _weapon.OnHitConfirmed -= HandleHitConfirmed;

            _cameraCore = null;
        }

        // Player가 실제로 장착한 무기에만 Local Camera를 연결한다.
        public void BindCamera(CameraCore p_cameraCore)
        {
            _cameraCore = p_cameraCore;
        }

        private void HandleHitConfirmed(int p_comboIndex)
        {
            if (_cameraCore == null ||
                p_comboIndex < 0 ||
                !_hitShakeByComboIndex.TryGetValue(
                    p_comboIndex,
                    out string shakeName))
            {
                return;
            }

            _cameraCore.RequestShake(shakeName);
        }

        private void RebuildHitShakeMap(bool p_logWarnings)
        {
            _hitShakeByComboIndex.Clear();

            if (_hitShakeBindings == null)
                return;

            for (int index = 0; index < _hitShakeBindings.Length; index++)
            {
                MeleeHitShakeBinding binding =
                    _hitShakeBindings[index];
                string shakeName = binding.ShakeName?.Trim();

                // 등록하지 않은 콤보와 비어 있는 항목은 Shake를 생략한다.
                if (binding.ComboIndex < 0 ||
                    string.IsNullOrWhiteSpace(shakeName))
                {
                    continue;
                }

                if (_hitShakeByComboIndex.ContainsKey(binding.ComboIndex))
                {
                    if (p_logWarnings)
                    {
                        Debug.LogWarning(
                            $"Melee Hit Shake의 Combo Index가 중복되었습니다: {binding.ComboIndex}",
                            this);
                    }

                    continue;
                }

                _hitShakeByComboIndex.Add(
                    binding.ComboIndex,
                    shakeName);
            }
        }

        private void OnValidate()
        {
            // Inspector 입력 중에는 경고 없이 현재 매칭만 갱신한다.
            RebuildHitShakeMap(false);
        }
    }
}
