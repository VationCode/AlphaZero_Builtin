using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 발사가 장착 Entity에 전달할 반동과 이동 영향 값을 보관한다.
    [Serializable]
    public sealed class RangeFireResponseSettings
    {
        [Tooltip("한 발을 발사할 때 전달할 반동 세기입니다.")]
        [SerializeField, Min(0f)]
        private float _recoil;

        [Tooltip("원거리 무기 사용 중 Player 이동에 적용할 속도 배율입니다.")]
        [SerializeField, Range(0f, 1f)]
        private float _moveSpeedMultiplier = 1f;

        [Tooltip("발사 시 Camera Entity에 요청할 Shake preset 이름입니다. 비우면 실행하지 않습니다.")]
        [SerializeField]
        private string _cameraShakeName = "Weak";

        public float Recoil => Mathf.Max(0f, _recoil);
        public float MoveSpeedMultiplier => Mathf.Clamp01(_moveSpeedMultiplier);
        public string CameraShakeName => _cameraShakeName;

        public void Validate()
        {
            _recoil = Mathf.Max(0f, _recoil);
            _moveSpeedMultiplier = Mathf.Clamp01(_moveSpeedMultiplier);
            _cameraShakeName = _cameraShakeName?.Trim() ?? string.Empty;
        }
    }
}
