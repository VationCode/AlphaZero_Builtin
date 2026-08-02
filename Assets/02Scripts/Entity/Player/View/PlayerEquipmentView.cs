using Alpha.Equipment;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    [Serializable]
    public class ArmorPivotBinding
    {
        [SerializeField] private EArmorType _armorType;
        [SerializeField] private Transform[] _pivots;

        public EArmorType ArmorType => _armorType;
        public Transform[] Pivots => _pivots;
    }

    // Player 손에 표시되는 현재 무기 외형을 관리한다.
    public class PlayerEquipmentView : MonoBehaviour
    {

        [Header("Armor Pivot")]
        [SerializeField] private ArmorPivotBinding[] _armorPivotBindings;

        [SerializeField] private Transform _weaponHandPivot;

        public GameObject CurrentWeaponInstance { get; private set; }

        private readonly Dictionary<EArmorType, List<GameObject>> _currentArmorInstanceDict = new();

        // 새 무기 외형을 만들고 런타임 Weapon을 반환한다.
        // 이 단계에서는 기존 무기를 제거하지 않는다.
        public bool TryCreateWeapon(GameObject p_prefab, out Weapon p_weapon)
        {
            p_weapon = null;

            if (p_prefab == null || _weaponHandPivot == null)
                return false;

            GameObject instance = Instantiate(p_prefab, _weaponHandPivot, false);

            instance.name = $"{p_prefab.name}_Equipped";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // 무기 Prefab의 Root에는 구체 Weapon 컴포넌트가 필요하다.
            if (!instance.TryGetComponent(out p_weapon))
            {
                Destroy(instance);
                return false;
            }

            return true;

        }

        // 초기화가 끝난 무기를 현재 외형으로 확정한다.
        public bool TryCommitWeapon(Weapon p_weapon)
        {
            if (p_weapon == null ||
                p_weapon.transform.parent != _weaponHandPivot)
            {
                return false;
            }

            GameObject previousInstance = CurrentWeaponInstance;
            CurrentWeaponInstance = p_weapon.gameObject;

            if (previousInstance != null &&
                previousInstance != CurrentWeaponInstance)
            {
                Destroy(previousInstance);
            }

            return true;
        }

        // 초기화에 실패한 임시 무기만 제거한다.
        public bool TryDiscardWeapon(Weapon p_weapon)
        {
            if (p_weapon == null || p_weapon.gameObject == CurrentWeaponInstance)
            {
                return false;
            }

            Destroy(p_weapon.gameObject);
            return true;
        }


        // 방어구 슬롯 변경
        // → EquipmentCore 이벤트
        // → PlayerEquipmentFlow
        // → 방어구 프리팹 로드
        // → PlayerEquipmentView 부위별 표시·제거
        public bool TryShowArmor(EArmorType p_armorType, GameObject p_prefab)
        {
            if (p_prefab == null || !TryGetArmorPivots(p_armorType, out Transform[] pivots))
            {
                return false;
            }

            List<GameObject> nextInstanceList = new(pivots.Length);

            foreach (Transform pivot in pivots)
            {
                GameObject instance =
                    Instantiate(p_prefab, pivot, false);

                instance.name =
                    $"{p_prefab.name}_{p_armorType}_Equipped";

                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                // 월드 아이템용 Collider는 장착 외형에서 사용하지 않는다.
                foreach (Collider itemCollider in
                         instance.GetComponentsInChildren<Collider>(true))
                {
                    itemCollider.enabled = false;
                }

                nextInstanceList.Add(instance);
            }

            TryClearArmor(p_armorType);
            _currentArmorInstanceDict[p_armorType] = nextInstanceList;

            return true;
        }
        public bool TryClearWeapon()
        {
            if (CurrentWeaponInstance == null)
                return false;

            Destroy(CurrentWeaponInstance);
            CurrentWeaponInstance = null;

            return true;
        }

        public bool TryClearArmor(EArmorType p_armorType)
        {
            if (!_currentArmorInstanceDict.TryGetValue(p_armorType, out List<GameObject> instances))
            {
                return false;
            }

            foreach (GameObject instance in instances)
            {
                if (instance != null)
                    Destroy(instance);
            }

            _currentArmorInstanceDict.Remove(p_armorType);

            return true;
        }

        private bool TryGetArmorPivots(EArmorType p_armorType, out Transform[] p_pivots)
        {
            // Inspector에 방어구 Pivot 배열이 설정되지 않은 경우를 방지한다.
            if (_armorPivotBindings == null)
            {
                p_pivots = null;
                return false;
            }

            foreach (ArmorPivotBinding binding in _armorPivotBindings)
            {
                if (binding == null || binding.ArmorType != p_armorType)
                {
                    continue;
                }

                Transform[] pivots = binding.Pivots;

                if (pivots == null || pivots.Length == 0)
                    break;

                foreach (Transform pivot in pivots)
                {
                    if (pivot == null)
                    {
                        p_pivots = null;
                        return false;
                    }
                }

                p_pivots = pivots;
                return true;
            }

            p_pivots = null;
            return false;
        }
    }
}
