using UnityEngine;
using UnityEngine.Serialization;

public class PlayerEquipmentView : MonoBehaviour
{
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private Transform _helmetPivot;
    [SerializeField] private Transform _chestPivot;
    [SerializeField] private Transform[] _glovesPivots;
    [SerializeField] private Transform[] _bootsPivots;

    private GameObject _weaponInstance;
    private GameObject _helmetInstance;
    private GameObject _chestInstance;

    private SlotBase _visibleWeaponSlot;
    private SlotBase _visibleHelmetSlot;
    private SlotBase _visibleChestSlot;

    public bool Supports(SlotBase p_slot)
    {
        if (p_slot is WeaponSlot)
            return true;

        if (p_slot is not ArmorSlot armorSlot)
            return false;

        return armorSlot.ArmorType == EArmorType.Helmet ||
               armorSlot.ArmorType == EArmorType.Chest;
    }

    public bool Equip(SlotBase p_slot, GameObject p_prefab)
    {
        if (p_slot == null || p_prefab == null)
            return false;

        if (p_slot is WeaponSlot)
        {
            return ReplaceVisual(
                p_slot,
                p_prefab,
                _weaponPivot,
                ref _weaponInstance,
                ref _visibleWeaponSlot);
        }

        if (p_slot is not ArmorSlot armorSlot)
            return false;

        switch (armorSlot.ArmorType)
        {
            case EArmorType.Helmet:
                return ReplaceVisual(
                    p_slot,
                    p_prefab,
                    _helmetPivot,
                    ref _helmetInstance,
                    ref _visibleHelmetSlot);

            case EArmorType.Chest:
                return ReplaceVisual(
                    p_slot,
                    p_prefab,
                    _chestPivot,
                    ref _chestInstance,
                    ref _visibleChestSlot);

            default:
                return false;
        }
    }

    public bool Unequip(SlotBase p_slot)
    {
        if (p_slot == null)
            return false;

        if (p_slot is WeaponSlot)
        {
            return RemoveVisual(p_slot, ref _weaponInstance, ref _visibleWeaponSlot);
        }

        if (p_slot is not ArmorSlot armorSlot) return false;

        switch (armorSlot.ArmorType)
        {
            case EArmorType.Helmet:
                return RemoveVisual(p_slot, ref _helmetInstance, ref _visibleHelmetSlot);

            case EArmorType.Chest:
                return RemoveVisual(p_slot, ref _chestInstance, ref _visibleChestSlot);

            default:
                return false;
        }
    }

    public void ClearAll()
    {
        DestroyInstance(_weaponInstance);
        DestroyInstance(_helmetInstance);
        DestroyInstance(_chestInstance);

        _weaponInstance = null;
        _helmetInstance = null;
        _chestInstance = null;

        _visibleWeaponSlot = null;
        _visibleHelmetSlot = null;
        _visibleChestSlot = null;
    }

    private bool ReplaceVisual(SlotBase p_sourceSlot,GameObject p_prefab,
                                Transform p_pivot, ref GameObject p_currentInstance,
                                ref SlotBase p_visibleSlot)
    {
        GameObject nextInstance = CreateInstance(p_prefab, p_pivot);

        // 새 장비 생성에 실패하면 기존 외형을 유지한다.
        if (nextInstance == null)
            return false;

        DestroyInstance(p_currentInstance);

        p_currentInstance = nextInstance;
        p_visibleSlot = p_sourceSlot;

        return true;
    }

    private bool RemoveVisual(SlotBase p_sourceSlot, ref GameObject p_currentInstance, ref SlotBase p_visibleSlot)
    {
        // 공용 WeaponPivot에 표시되지 않은 다른 무기 슬롯이
        // 해제되더라도 현재 표시 중인 무기는 제거하지 않는다.
        if (!ReferenceEquals(p_sourceSlot, p_visibleSlot))
            return false;

        DestroyInstance(p_currentInstance);

        p_currentInstance = null;
        p_visibleSlot = null;

        return true;
    }

    private GameObject CreateInstance(
        GameObject p_prefab,
        Transform p_pivot)
    {
        if (p_pivot == null)
        {
            Debug.LogError(
                $"[PlayerEquipmentView] {p_prefab.name}의 Pivot이 없습니다.",
                this);

            return null;
        }

        GameObject instance = Instantiate(
            p_prefab,
            p_pivot,
            false);

        instance.name = $"{p_prefab.name}_Equipped";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        PrepareAsEquippedObject(instance);

        // 비활성화된 프리팹도 장착 시 표시한다.
        instance.SetActive(true);

        return instance;
    }

    private void PrepareAsEquippedObject(GameObject p_instance)
    {
        Collider[] colliders =
            p_instance.GetComponentsInChildren<Collider>(true);

        foreach (Collider equipmentCollider in colliders)
        {
            equipmentCollider.enabled = false;
        }

        Rigidbody[] rigidbodies =
            p_instance.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody equipmentRigidbody in rigidbodies)
        {
            equipmentRigidbody.useGravity = false;
            equipmentRigidbody.isKinematic = true;
            equipmentRigidbody.detectCollisions = false;
        }
    }

    private void DestroyInstance(GameObject p_instance)
    {
        if (p_instance != null)
        {
            Destroy(p_instance);
        }
    }

    private void OnDestroy()
    {
        ClearAll();
    }
}
