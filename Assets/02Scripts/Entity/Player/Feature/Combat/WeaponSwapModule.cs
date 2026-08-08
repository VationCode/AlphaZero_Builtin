using Alpha.Item.Weapon;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // 전달된 WeaponDTO에 맞는 실제 무기 오브젝트를 생성하거나 제거한다.
    public class WeaponSwapModule : MonoBehaviour
    {
        [SerializeField] private Transform _weaponRoot;

        private ResourceLoadSystem _resourceLoader;
        private GameObject _currentWeaponObject;

        // 현재 생성되고 초기화된 전투용 무기다.
        public Weapon CurrentWeapon { get; private set; }

        // Prefab 조회 시스템과 실제 무기 생성 위치를 준비한다.
        public bool Bind(ResourceLoadSystem p_resourceLoader)
        {
            if (p_resourceLoader == null)
            {
                Debug.LogError($"{nameof(WeaponSwapModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _resourceLoader = p_resourceLoader;
            _weaponRoot ??= transform;  // _weaponRoot가 null일경우 transform으로
            return true;
        }

        // 장착된 무기는 새 Prefab으로 교체하고, 해제된(null이면) 현재 무기는 제거한다.
        public bool Apply(WeaponDTO p_weapon)
        {
            if (_resourceLoader == null)
                return false;

            if (p_weapon == null)
            {
                ClearCurrentWeapon();
                return true;
            }

            GameObject prefab = _resourceLoader.GetItemPrefab(p_weapon.ItemType, p_weapon.PrefabKey);

            if (prefab == null)
                return false;

            GameObject nextWeaponObject = Instantiate(prefab, _weaponRoot, false);

            // 런타임 Weapon이 있는 Prefab은 DTO까지 함께 초기화한다.
            Weapon nextWeapon = nextWeaponObject.GetComponent<Weapon>();

            if (nextWeapon == null || !nextWeapon.TryInitialize(p_weapon))
            {
                Destroy(nextWeaponObject);
                return false;
            }

            // 새 무기 준비가 끝난 후 기존 무기를 제거한다.
            ClearCurrentWeapon();

            _currentWeaponObject = nextWeaponObject;
            CurrentWeapon = nextWeapon;

            return true;
        }

        // 현재 생성된 실제 무기를 제거한다.
        private void ClearCurrentWeapon()
        {
            if (_currentWeaponObject != null)
                Destroy(_currentWeaponObject);

            _currentWeaponObject = null;
            CurrentWeapon = null;
        }

        // 객체 해제 시 생성한 실제 무기를 함께 정리한다.
        private void OnDestroy()
        {
            ClearCurrentWeapon();
        }
    }
}
