using UnityEngine;

namespace Alpha.Player.Equipment
{
    // Player 손에 표시되는 현재 무기 외형을 관리한다.
    public class PlayerEquipmentView : MonoBehaviour
    {
        [SerializeField] private Transform _handPivot;

        public GameObject CurrentWeaponInstance { get; private set; }

        public bool TryShowWeapon(GameObject p_prefab)
        {
            if (p_prefab == null || _handPivot == null)
                return false;

            GameObject nextInstance = Instantiate(p_prefab, _handPivot, false);

            nextInstance.name = $"{p_prefab.name}_Equipped";
            nextInstance.transform.localPosition = Vector3.zero;
            nextInstance.transform.localRotation = Quaternion.identity;

            GameObject previousInstance = CurrentWeaponInstance;
            CurrentWeaponInstance = nextInstance;

            if (previousInstance != null)
                Destroy(previousInstance);

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
    }
}
