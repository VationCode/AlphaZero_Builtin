using UnityEngine;

public class PlayerEquipmentFlow : MonoBehaviour
{
    private PlayerEquipmentModule _equipmentModule;
    private PlayerEquipmentView _equipmentView;
    private ResourceLoadSystem _resourceLoader;

    public void Bind(PlayerEquipmentModule p_equipmentModule,
                    PlayerEquipmentView p_equipmentView, 
                    ResourceLoadSystem p_resourceLoader)
    {
        Unbind();

        _equipmentModule = p_equipmentModule;
        _equipmentView = p_equipmentView;
        _resourceLoader = p_resourceLoader;

        if (_equipmentModule == null || _equipmentView == null || _resourceLoader == null)
        {
            Debug.LogError(
                "[PlayerEquipmentFlow] 필수 의존성이 연결되지 않았습니다.",
                this);

            return;
        }

        _equipmentModule.EquipmentChanged += OnEquipmentChanged;

        foreach (SlotBase slot in _equipmentModule.SlotList)
        {
            OnEquipmentChanged(slot);
        }
    }

    public bool TrySelectWeapon(int p_swapNum)
    {
        EWeaponType weaponType;

        switch (p_swapNum)
        {
            case 0:
                weaponType = EWeaponType.Melee;
                break;

            case 1:
                weaponType = EWeaponType.Range;
                break;

            case 2:
                weaponType = EWeaponType.Special;
                break;

            default:
                return false;
        }

        return SelectWeapon(weaponType);
    }
    private bool SelectWeapon(EWeaponType p_weaponType)
    {
        if (_equipmentModule.ActiveWeaponType == p_weaponType)
            return false;

        WeaponSlot weaponSlot =
            _equipmentModule.GetWeaponSlot(p_weaponType);

        if (weaponSlot == null || weaponSlot.IsEmpty)
            return false;

        if (!TryEquipSlot(weaponSlot))
            return false;

        return _equipmentModule.TrySelectWeapon(p_weaponType);
    }
    private void OnEquipmentChanged(SlotBase p_slot)
    {
        if (p_slot == null || !_equipmentView.Supports(p_slot))
            return;

        if (p_slot is ArmorSlot)
        {
            SynchronizeArmor(p_slot);
            return;
        }

        if (p_slot is not WeaponSlot weaponSlot)
            return;

        if (p_slot.IsEmpty)
        {
            OnWeaponRemoved(weaponSlot);
            return;
        }

        // 처음 장착한 무기는 자동으로 활성 무기가 된다.
        if (_equipmentModule.ActiveWeaponType == EWeaponType.None)
        {
            SelectWeapon(weaponSlot.WeaponType);
            return;
        }

        // 현재 사용 중인 슬롯의 아이템이 교체된 경우 외형도 교체한다.
        if (_equipmentModule.ActiveWeaponType == weaponSlot.WeaponType)
        {
            TryEquipSlot(weaponSlot);
        }
    }

    private void SynchronizeArmor(SlotBase p_slot)
    {
        if (p_slot.IsEmpty)
        {
            _equipmentView.Unequip(p_slot);
            return;
        }

        TryEquipSlot(p_slot);
    }

    private void OnWeaponRemoved(WeaponSlot p_weaponSlot)
    {
        // 비활성 무기 슬롯에서 제거된 경우 현재 무기에는 영향이 없다.
        if (_equipmentModule.ActiveWeaponType != p_weaponSlot.WeaponType)
            return;

        _equipmentView.Unequip(p_weaponSlot);

        if (!TrySelectFallbackWeapon())
        {
            _equipmentModule.ClearActiveWeapon();
        }
    }



    private bool TrySelectFallbackWeapon()
    {
        foreach (SlotBase slot in _equipmentModule.SlotList)
        {
            if (slot is not WeaponSlot weaponSlot ||
                weaponSlot.IsEmpty)
            {
                continue;
            }

            if (SelectWeapon(weaponSlot.WeaponType))
                return true;
        }

        return false;
    }

    private bool TryEquipSlot(SlotBase p_slot)
    {
        if (p_slot == null || p_slot.IsEmpty)
            return false;

        ItemDTO item = p_slot.Item;

        GameObject prefab = _resourceLoader.GetItemPrefab(item.ItemType, item.PrefabKey);

        if (prefab == null)
            return false;

        return _equipmentView.Equip(p_slot, prefab);
    }

    private void Unbind()
    {
        if (_equipmentModule != null)
        {
            _equipmentModule.EquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
