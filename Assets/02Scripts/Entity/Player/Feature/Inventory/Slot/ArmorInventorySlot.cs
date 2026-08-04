using UnityEngine;
namespace Alpha.Player.Inventory
{
    public class ArmorInventorySlot : InventorySlot
    {
        public EArmorType ArmorType { get; }

        public ArmorInventorySlot(int p_index, EArmorType p_armorType) : base(p_index)
        {
            ArmorType = p_armorType;
        }

        public override bool CanStore(ItemDTO p_item)
        {
            return p_item is ArmorDTO armor &&
                   armor.ArmorType == ArmorType;
        }
    }
}
