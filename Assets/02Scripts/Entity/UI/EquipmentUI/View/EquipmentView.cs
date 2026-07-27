using UnityEngine;

namespace Alpha.Equipment
{
    // 장비 슬롯의 Unity UI 참조를 보유한다.
    public class EquipmentView : MonoBehaviour
    {
        [SerializeField] private EquipmentWeaponPageView _weaponPage;
        [SerializeField] private EquipmentArmorPageView _armorPage;

        public bool TryGetWeaponPage(out EquipmentWeaponPageView p_pageView)
        {
            p_pageView = _weaponPage;

            return p_pageView != null;
        }

        public bool TryGetArmorPage(out EquipmentArmorPageView p_pageView)
        {
            p_pageView = _armorPage;

            return p_pageView != null;
        }
    }
}