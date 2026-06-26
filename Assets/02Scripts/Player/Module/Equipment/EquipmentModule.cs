using UnityEngine;

public class EquipmentModule : MonoBehaviour
{
    [SerializeField] private WeaponDB weaponDB;

    private void Start()
    {
        Equip(1002);
    }

    public void Equip(int p_weaponID)
    {
        // 무기 정보 받아오기
        /*WeaponDataDTO weaponData = weaponDB.GetWeaponData(p_weaponID);

        Sprite icon = ResourceDBLoadModule.Instance.GetIcon(weaponData.IconKey);
        GameObject prefab = 
            ResourceDBLoadModule.Instance.GetWeaponPrefab(weaponData.PrefabKey);

        Instantiate(prefab, Vector3.up, Quaternion.identity);*/
    }
}
