using UnityEngine;

// 모든 런타임 무기의 공통 진입점

namespace Alpha.Item.Weapon
{
    // 모든 런타임 무기의 DTO 검증과 공통 초기화 생명주기를 제공한다.
    public abstract class Weapon : MonoBehaviour
    {
        public WeaponDTO Data { get; private set; }

        public IWeaponAction PrimaryAction { get; protected set; }

        public IWeaponAction SecondaryAction { get; protected set; }

        public bool IsInitialized { get; private set; }

        // 구체 무기와 DTO 타입을 검증한 뒤 한 번만 초기화한다.
        public bool TryInitialize(WeaponDTO p_data)
        {
            if (p_data == null || !CanInitialize(p_data))
                return false;

            // 동일 데이터의 중복 초기화는 성공으로 처리한다.
            if (IsInitialized)
                return ReferenceEquals(Data, p_data);

            Data = p_data;

            OnInitialized();

            IsInitialized = true;
            return true;
        }


        // 구체 무기가 자신에게 맞는 DTO인지 검사한다.
        protected abstract bool CanInitialize(WeaponDTO p_data);

        // 구체 무기가 추가 초기화를 수행할 수 있다.
        protected virtual void OnInitialized()
        {
        }
    }
}
