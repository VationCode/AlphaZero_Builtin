using UnityEngine;

public class ArmorEquipmentView : MonoBehaviour
{
    [Header("Armor Slots")]
    [SerializeField] private SlotBaseUI _helmetSlot;
    [SerializeField] private SlotBaseUI _chestSlot;
    [SerializeField] private SlotBaseUI _glovesSlot;
    [SerializeField] private SlotBaseUI _bootsSlot;

    public SlotBaseUI GetArmorSlot(EArmorType p_armorType)
    {
        switch (p_armorType)
        {
            case EArmorType.Helmet:
                return _helmetSlot;

            case EArmorType.Chest:
                return _chestSlot;

            case EArmorType.Gloves:
                return _glovesSlot;

            case EArmorType.Boots:
                return _bootsSlot;

            default:
                return null;
        }
    }
}
