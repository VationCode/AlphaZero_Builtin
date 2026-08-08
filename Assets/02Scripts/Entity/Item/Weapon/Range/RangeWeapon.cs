using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 원거리 무기의 공통 연속 발사와 조준 입력 생명주기를 담당한다.
    public abstract class RangeWeapon : Weapon
    {
        [Header("Action")]
        [SerializeField]
        private EWeaponInputMode _primaryInputMode = EWeaponInputMode.Auto;

        private float _fireCooldown;

        public bool IsAiming { get; private set; }
        protected RangeWeaponDTO RangeData => Data as RangeWeaponDTO;

        protected sealed override bool CanInitialize(WeaponDTO p_data)
        {
            return p_data is RangeWeaponDTO;
        }

        // 좌클릭은 연속 발사를 준비하고 우클릭은 조준 상태에 진입한다.
        protected override bool OnBeginAction(EWeaponActionType p_type)
        {
            switch (p_type)
            {
                case EWeaponActionType.Primary:
                    Fire();
                    _fireCooldown = Mathf.Max(0.01f, RangeData.Rate);
                    return true;

                case EWeaponActionType.Secondary:
                    SetAiming(true);
                    return true;

                default:
                    return false;
            }
        }

        // 좌클릭을 유지하는 동안 DTO의 Rate 간격으로 발사한다.
        protected override void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (p_type != EWeaponActionType.Primary || RangeData == null)
                return;

            // Semi는 시작할 때 한 발만 발사하고 행동을 종료한다.
            if (_primaryInputMode == EWeaponInputMode.Semi)
            {
                EndAction();
                return;
            }

            _fireCooldown -= p_deltaTime;

            if (_fireCooldown > 0f)
                return;

            Fire();
            _fireCooldown = Mathf.Max(0.01f, RangeData.Rate);
        }

        protected override void OnEndAction(EWeaponActionType p_type)
        {
            ResetAction(p_type);
        }

        protected override void OnCancelAction(EWeaponActionType p_type)
        {
            ResetAction(p_type);
        }

        // 실제 명중과 Damage 처리는 이후 구체적인 사격 기능에서 확장한다.
        protected virtual void Fire()
        {
            Debug.DrawRay(
                transform.position,
                transform.forward * RangeData.MaxDistance,
                Color.red,
                0.1f);
        }

        // Rifle과 Sniper의 조준·줌 표현 차이를 확장할 수 있다.
        protected virtual void OnAimChanged(bool p_isAiming) { }

        private void ResetAction(EWeaponActionType p_type)
        {
            if (p_type == EWeaponActionType.Primary)
                _fireCooldown = 0f;

            if (p_type == EWeaponActionType.Secondary)
                SetAiming(false);
        }

        private void SetAiming(bool p_isAiming)
        {
            IsAiming = p_isAiming;
            OnAimChanged(p_isAiming);
        }
    }
}
