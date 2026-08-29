using UnityEngine;

// 모든 런타임 무기의 공통 진입점

namespace Alpha.Item.Weapon
{
    // 무기의 좌·우 입력에 대응하는 공통 행동 종류다.
    public enum EWeaponActionType
    {
        None,
        Primary,
        Secondary
    }

    // 모든 런타임 무기의 DTO 검증과 공통 초기화 생명주기를 제공한다.
    public abstract class Weapon : MonoBehaviour
    {
        public WeaponDTO Data { get; private set; }
        public bool IsInitialized { get; private set; }
        public abstract EWeaponType WeaponType { get; }

        public EWeaponActionType ActiveActionType { get; private set; } = EWeaponActionType.None;

        public bool HasActiveAction => ActiveActionType != EWeaponActionType.None;

        // 기본 무기 행동은 입력을 놓으면 종료된다.
        public virtual bool EndsOnInputRelease(EWeaponActionType p_type)
        {
            return true;
        }

        // 구체 무기 계열이 자신에게 맞는 DTO인지 검사한다.
        protected abstract bool CanInitialize(WeaponDTO p_data);

        // 구체 무기 계열이 초기화 후 추가 상태를 준비할 수 있다.
        protected virtual void OnInitialized() { }

        // 무기 계열이 좌·우 행동의 실제 시작과 갱신을 구현한다.
        protected abstract bool OnBeginAction(EWeaponActionType p_type);
        protected abstract void OnTickAction(
            EWeaponActionType p_type,
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime);
        protected virtual void OnEndAction(EWeaponActionType p_type) { }
        protected virtual void OnCancelAction(EWeaponActionType p_type) { }

        // CSV의 Type과 Prefab의 Type 계약을 확인하고 한 번만 초기화한다.
        public bool TryInitialize(WeaponDTO p_data)
        {
            if (p_data == null ||
                WeaponType == EWeaponType.None ||
                p_data.WeaponType != WeaponType ||
                !CanInitialize(p_data))
            {
                return false;
            }

            if (IsInitialized)
                return ReferenceEquals(Data, p_data);

            Data = p_data;
            OnInitialized();

            IsInitialized = true;
            return true;
        }

        // 현재 무기의 선택된 좌·우 행동을 시작한다.
        public bool TryBeginAction(EWeaponActionType p_type)
        {
            if (!IsInitialized ||
                p_type == EWeaponActionType.None ||
                HasActiveAction ||
                !OnBeginAction(p_type))
            {
                return false;
            }

            ActiveActionType = p_type;
            return true;
        }

        // 진행 중인 행동을 매 프레임 갱신한다.
        public void TickAction(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            if (!HasActiveAction)
                return;

            EWeaponActionType actionType = ActiveActionType;

            if (EndsOnInputRelease(actionType) && !p_isInputHeld)
            {
                EndAction();
                return;
            }

            OnTickAction(
                actionType,
                p_isInputHeld,
                p_isInputPressed,
                p_deltaTime);
        }

        // 정상적인 입력 해제로 현재 행동을 종료한다.
        public void EndAction()
        {
            if (!HasActiveAction)
                return;

            OnEndAction(ActiveActionType);
            ActiveActionType = EWeaponActionType.None;
        }

        // 무기 교체나 행동 제한으로 현재 행동을 강제 취소한다.
        public void CancelAction()
        {
            if (!HasActiveAction)
                return;

            OnCancelAction(ActiveActionType);
            ActiveActionType = EWeaponActionType.None;
        }
    }
}
