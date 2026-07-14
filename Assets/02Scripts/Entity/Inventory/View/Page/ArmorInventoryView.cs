using UnityEngine;

public class ArmorInventoryView : ViewBase
{
    [SerializeField]
    private SlotBaseUI _slotUIPrefab;

    [Header("Horizontal Scroll Contents")]
    [SerializeField]
    private Transform _helmetContent;

    [SerializeField]
    private Transform _chestContent;

    [SerializeField]
    private Transform _glovesContent;

    [SerializeField]
    private Transform _bootsContent;

    public SlotBaseUI CreateSlotView(EArmorType p_armorType)
    {
        Transform content = GetContent(p_armorType);

        if (content == null || _slotUIPrefab == null)
            return null;

        return Instantiate(_slotUIPrefab, content);
    }

    private Transform GetContent(EArmorType p_armorType)
    {
        switch (p_armorType)
        {
            case EArmorType.Helmet:
                return _helmetContent;

            case EArmorType.Chest:
                return _chestContent;

            case EArmorType.Gloves:
                return _glovesContent;

            case EArmorType.Boots:
                return _bootsContent;

            default:
                return null;
        }
    }
}
