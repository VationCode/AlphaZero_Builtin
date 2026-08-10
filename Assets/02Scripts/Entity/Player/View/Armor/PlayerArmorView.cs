using System.Collections.Generic;
using Alpha.Item.Armor;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // Armor 장비 슬롯의 현재 상태를 Player Bone 위의 월드 표현으로 반영한다.
    public sealed class PlayerArmorView : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private readonly Dictionary<EArmorType, ArmorItem>
            _equippedArmorItems = new();

        private EquipmentContext _context;
        private ResourceLoadSystem _resourceLoader;

        private void Awake()
        {
            _animator ??= GetComponent<Animator>();
        }

        public bool Bind(
            EquipmentContext p_context,
            ResourceLoadSystem p_resourceLoader)
        {
            if (p_context == null ||
                p_resourceLoader == null ||
                _animator == null)
            {
                return false;
            }

            Unbind();

            _context = p_context;
            _resourceLoader = p_resourceLoader;
            _context.OnSlotChanged += HandleSlotChanged;

            foreach (EquipmentSlot slot in _context.Slots)
            {
                if (slot is ArmorEquipmentSlot armorSlot)
                    ApplyArmorSlot(armorSlot);
            }

            return true;
        }

        public void Unbind()
        {
            if (_context != null)
                _context.OnSlotChanged -= HandleSlotChanged;

            _context = null;
            _resourceLoader = null;

            foreach (ArmorItem armorItem in _equippedArmorItems.Values)
            {
                if (armorItem == null)
                    continue;

                armorItem.gameObject.SetActive(false);
                Destroy(armorItem.gameObject);
            }

            _equippedArmorItems.Clear();
        }

        private void HandleSlotChanged(EquipmentSlot p_slot)
        {
            if (p_slot is ArmorEquipmentSlot armorSlot)
                ApplyArmorSlot(armorSlot);
        }

        private void ApplyArmorSlot(ArmorEquipmentSlot p_slot)
        {
            RemoveArmor(p_slot.ArmorType);

            ArmorDTO armorData = p_slot.Armor;

            if (armorData == null)
                return;

            Transform targetBone = ResolveTargetBone(
                armorData.ArmorType);

            if (targetBone == null)
            {
                Debug.LogWarning(
                    $"지원되는 Armor Bone이 없습니다: {armorData.ArmorType}",
                    this);
                return;
            }

            GameObject prefab = _resourceLoader.GetItemPrefab(
                EItemType.Armor,
                armorData.PrefabKey);

            if (prefab == null ||
                prefab.GetComponent<ArmorItem>() == null)
            {
                Debug.LogError(
                    $"ArmorItem이 설정된 Prefab이 없습니다: {armorData.PrefabKey}",
                    this);
                return;
            }

            GameObject instance = Instantiate(
                prefab,
                targetBone,
                false);

            ArmorItem armorItem = instance.GetComponent<ArmorItem>();

            if (!armorItem.TryInitialize(armorData))
            {
                Destroy(instance);
                return;
            }

            DisableColliders(instance);
            _equippedArmorItems[armorData.ArmorType] = armorItem;
        }

        private void RemoveArmor(EArmorType p_armorType)
        {
            if (!_equippedArmorItems.Remove(
                    p_armorType,
                    out ArmorItem armorItem) ||
                armorItem == null)
            {
                return;
            }

            armorItem.gameObject.SetActive(false);
            Destroy(armorItem.gameObject);
        }

        private Transform ResolveTargetBone(EArmorType p_armorType)
        {
            switch (p_armorType)
            {
                case EArmorType.Helmet:
                    return _animator.GetBoneTransform(
                        HumanBodyBones.Head);

                case EArmorType.Chest:
                    return _animator.GetBoneTransform(
                               HumanBodyBones.Chest) ??
                           _animator.GetBoneTransform(
                               HumanBodyBones.Spine);

                default:
                    return null;
            }
        }

        private static void DisableColliders(GameObject p_instance)
        {
            Collider[] colliders =
                p_instance.GetComponentsInChildren<Collider>(true);

            foreach (Collider armorCollider in colliders)
                armorCollider.enabled = false;
        }
    }
}
