using System;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    [Serializable]
    public sealed class BossData
    {
        [Header("Locomotion")]
        [SerializeField, Min(0f)]
        private float _moveSpeed = 5f;

        [SerializeField, Min(0f)]
        [Tooltip("초당 회전 각도")]
        private float _turnSpeed = 180f;

        [Header("Targeting")]
        [SerializeField, Min(0f)]
        [Tooltip("타겟을 처음 발견하는 거리")]
        private float _targetDetectionDistance = 60f;

        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float TargetDetectionDistance => _targetDetectionDistance;
    }
}