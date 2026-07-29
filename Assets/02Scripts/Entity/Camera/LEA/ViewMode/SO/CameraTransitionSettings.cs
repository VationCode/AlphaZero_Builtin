using System;
using UnityEngine;

namespace Alpha.AlphaCamera
{
    // 출발 View와 도착 View 조합에 따른 전환 시간을 관리한다.
    [Serializable]
    public class CameraTransitionSettings
    {
        [SerializeField, Min(0f)] private float _tpsAimDuration = 0.18f;

        [SerializeField, Min(0f)] private float _quarterDuration = 0.45f;

        public float ResolveDuration(ECameraViewType p_from, ECameraViewType p_to)
        {
            if (p_from == p_to)
                return 0f;

            // Quarter가 포함된 전환은 구도 변화가 크므로 조금 더 여유 있게 처리한다.
            if (p_from == ECameraViewType.Quarter || p_to == ECameraViewType.Quarter)
            {
                return Mathf.Max(0f, _quarterDuration);
            }

            // TPS와 Aim 간 전환은 전투 반응성을 위해 빠르게 처리한다.
            return Mathf.Max(0f, _tpsAimDuration);
        }
    }
}
