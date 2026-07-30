using System;

namespace Alpha.Player.Combat
{
    public enum EAttackMovePolicy
    {
        Free,             // 일반 이동 허용
        Locked,           // 이동 입력 차단
        AnimationDriven   // Root Motion으로 이동
    }

    public enum EAttackAnimationPolicy
    {
        UpperBody,        // 이동 애니메이션 유지
        FullBody          // 전신 공격 애니메이션
    }

    public enum EAttackRotationPolicy
    {
        TrackAim,         // 계속 Aim 방향 추적
        WindupOnly,       // 준비 구간까지만 추적
        Locked            // 공격 시작 방향 유지
    }

    public enum EAttackDeliveryType
    {
        MeleeHitbox,
        HitScan,
        Projectile
    }

    [Serializable]
    public class AttackDefinition
    {
        public string AnimationKey;

        public EAttackMovePolicy MovePolicy;
        public EAttackAnimationPolicy AnimationPolicy;
        public EAttackRotationPolicy RotationPolicy;
        public EAttackDeliveryType DeliveryType;

        public float Cooldown;
    }
}