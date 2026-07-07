using UnityEngine;

public class WeaponInvenSlot : SlotBase
{
    public WeaponDTO WeaponData { get; private set; }
    public override void SetItem(ItemDataDTO p_ItemData)
    {
        base.SetItem(p_ItemData);
        WeaponData = p_ItemData as WeaponDTO;
    }

    public override void Clear()
    {
        base.Clear();
    }
}
