using UnityEngine;

public class ArmorSlot : SlotBase
{
    public EArmorType ArmorType { get; }

    public ArmorSlot(EArmorType armorType)
    {
        ArmorType = armorType;
    }

    public override bool SetItem(ItemDTO p_item, int p_count = 1)
    {
        if (p_item is not ArmorDTO armor)
            return false;

        if (armor.ArmorType != ArmorType)
            return false;

        return base.SetItem(p_item, 1);
    }
}
