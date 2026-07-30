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

        public bool TryShowWeapon(GameObject p_prefab)
        {
            if (p_prefab == null || _weaponHandPivot == null)
                return false;

            GameObject nextInstance = Instantiate(p_prefab, _weaponHandPivot, false);

            nextInstance.name = $"{p_prefab.name}_Equipped";
            nextInstance.transform.localPosition = Vector3.zero;
            nextInstance.transform.localRotation = Quaternion.identity;

            GameObject previousInstance = CurrentWeaponInstance;
            CurrentWeaponInstance = nextInstance;

            if (previousInstance != null)
                Destroy(previousInstance);

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
