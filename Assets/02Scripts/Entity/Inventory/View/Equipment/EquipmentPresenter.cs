using System.Collections.Generic;

public class EquipmentPresenter
{
    private readonly PlayerEquipmentModule _equipmentModule;
    private readonly EquipmentView _equipmentView;
    private readonly ResourceLoadSystem _resourceLoader;
    private readonly SlotTransferSystem _transferSystem;

    private readonly List<SlotPresenter> _slotPresenters = new();

    public EquipmentPresenter
        (PlayerEquipmentModule p_equipmentModule, EquipmentView p_equipmentView, 
        ResourceLoadSystem p_resourceLoader, SlotTransferSystem p_transferSystem)
    {
        _equipmentModule = p_equipmentModule;
        _equipmentView = p_equipmentView;
        _resourceLoader = p_resourceLoader;
        _transferSystem = p_transferSystem;
    }

    public void Initialize()
    {
        foreach (SlotBase slot in _equipmentModule.SlotList)
        {
            SlotBaseUI slotView = GetSlotView(slot);

            if (slotView == null)
                continue;

            SlotPresenter presenter = 
                new SlotPresenter(slot, slotView, _resourceLoader, _transferSystem);

            _slotPresenters.Add(presenter);
            presenter.Refresh();
        }

        _equipmentModule.EquipmentChanged += OnEquipmentChanged;
    }

    public void Refresh()
    {
        foreach (SlotPresenter presenter in _slotPresenters)
        {
            presenter.Refresh();
        }
    }

    private SlotBaseUI GetSlotView(SlotBase p_slot)
    {
        if (p_slot is WeaponSlot weaponSlot)
        {
            return _equipmentView.WeaponView.GetWeaponSlot(
                weaponSlot.WeaponType);
        }

        if (p_slot is ArmorSlot armorSlot)
        {
            return _equipmentView.ArmorView.GetArmorSlot(
                armorSlot.ArmorType);
        }

        return null;
    }

    private void OnEquipmentChanged(SlotBase p_slot)
    {
        Refresh();
    }
}
