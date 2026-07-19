using Alpha.UI;
using UnityEngine;

namespace Alpha.Inventory
{
    public class WeaponInventoryView : ViewBase
    {
        [SerializeField]
        private SlotBaseUI _slotPrefab;

        [Header("Horizontal Scroll Contents")]
        [SerializeField]
        private Transform _meleeContent;

        [SerializeField]
        private Transform _rangeContent;

        [SerializeField]
        private Transform _specialContent;

        public SlotBaseUI CreateSlotView(EWeaponType p_weaponType)
        {
            Transform content = GetContent(p_weaponType);

            if (content == null || _slotPrefab == null)
                return null;

            return Instantiate(_slotPrefab, content);
        }

        private Transform GetContent(EWeaponType p_weaponType)
        {
            switch (p_weaponType)
            {
                case EWeaponType.Melee:
                    return _meleeContent;

                case EWeaponType.Range:
                    return _rangeContent;

                case EWeaponType.Special:
                    return _specialContent;

                default:
                    return null;
            }
        }
    }
}
