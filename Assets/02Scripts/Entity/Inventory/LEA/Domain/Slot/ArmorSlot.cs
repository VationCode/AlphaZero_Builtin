using UnityEngine;

public class ArmorSlot : SlotBase
{
    public EArmorType ArmorType { get; }

    public ArmorSlot(EArmorType p_armorType)
    {
        ArmorType = p_armorType;
    }

    public override bool CanStore(ItemDTO p_item)
    {
        if (p_item is not ArmorDTO armor) return false;

        return armor.ArmorType == ArmorType;
    }
}
