using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : UIPanel
{
    public Transform MeleeContent;
    public Transform RangeContent;
    public Transform SPRangeContent;

    public GameObject slotPrefab;

    private List<WeaponInvenSlot> meleeSlots = new List<WeaponInvenSlot>();
    private List<WeaponInvenSlot> rangeSlots = new List<WeaponInvenSlot>();
    private List<WeaponInvenSlot> spRangeSlots = new List<WeaponInvenSlot>();
    public void AddItem(WeaponDTO weaponData)
    {
        switch (weaponData.WeaponType)
        {
            case EWeaponType.Melee:
                meleeSlots.Add(Instantiate(slotPrefab, MeleeContent).GetComponent<WeaponInvenSlot>());
                break;
            case EWeaponType.Range:
                rangeSlots.Add(Instantiate(slotPrefab, RangeContent).GetComponent<WeaponInvenSlot>());
                break;
            case EWeaponType.SPRange:
                spRangeSlots.Add(Instantiate(slotPrefab, SPRangeContent).GetComponent<WeaponInvenSlot>());
                break;
        }
    }
    public override void Open()
    {
        base.Open();
    }
    public override void Close()
    {
        base.Close();
    }
}
