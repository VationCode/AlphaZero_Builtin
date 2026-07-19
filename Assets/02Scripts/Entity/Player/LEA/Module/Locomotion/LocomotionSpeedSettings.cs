using System;
using UnityEngine;

namespace Alpha.Player
{
    [Serializable]
    public class LocomotionSpeedSettings
    {
        [SerializeField, Min(0f)]
        private float _normalSpeed = 3f;

        [SerializeField, Min(0f)]
        private float _fastSpeed = 5f;

        [SerializeField, Min(0f)]
        private float _combatSpeed = 2f;

        public float NormalSpeed => _normalSpeed;
        public float FastSpeed => _fastSpeed;
        public float CombatSpeed => _combatSpeed;

        public float GetSpeed(bool p_isFast, bool p_isCombat)
        {
            if (p_isCombat) return _combatSpeed;

            return p_isFast? _fastSpeed : _normalSpeed;
        }
    }
}
