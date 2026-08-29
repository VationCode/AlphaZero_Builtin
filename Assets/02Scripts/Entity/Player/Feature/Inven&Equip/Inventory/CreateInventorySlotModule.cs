using System;

namespace Alpha.Player.Inventory
{
    // 슬롯 생성과 확장을 담당
    public class CreateInventorySlotModule
    {
        private int _weaponSlotCount;
        private int _armorSlotCount;
        private int _commonSlotCount;

        private InventoryContext _context;
        private int _nextSlotIndex;

        // 전달받은 값으로 초기 상태를 구성한다.
        public CreateInventorySlotModule(int p_weaponSlotCount, int p_armorSlotCount, int p_commonSlotCount)
        {
            _weaponSlotCount = p_weaponSlotCount;
            _armorSlotCount = p_armorSlotCount;
            _commonSlotCount = p_commonSlotCount;
        }

        // 기존 슬롯을 비우고 아이템 분류별 기본 슬롯을 다시 생성한다.
        public bool Initialize(InventoryContext p_context)
        {
            if (p_context == null)
                return false;

            _context = p_context;

            // 재초기화 시 기존 슬롯과 Index를 먼저 초기화한다.
            _context.Clear();

            _nextSlotIndex = 0;

            // 아이템 분류별 설정 수량만큼 기본 슬롯을 생성한다.
            CreateWeaponSlots();
            CreateArmorSlots();
            CreateConsumableSlots();
            CreateMaterialSlots();
            CreateQuestItemSlots();

            return true;
        }

        // 버튼에 의한 슬롯 추가
        public bool AddSlot(EItemType p_itemType, int p_groupIndex)
        {
            if (_context == null || p_groupIndex < 0)
                return false;

            InventorySlot slot = CreateSlot(p_itemType, p_groupIndex);

            if (slot == null)
                return false;

            _context.AddSlot(p_itemType, slot);

            return true;
        }

        // 아이템 종류와 세부 그룹에 맞는 슬롯 구현체를 생성한다.
        private InventorySlot CreateSlot(EItemType p_itemType, int p_groupIndex)
        {
            // 분류형 아이템은 세부 Category 슬롯, QuestItem은 공용 슬롯을 사용한다.
            switch (p_itemType)
            {
                case EItemType.Weapon:
                    if (!Enum.IsDefined(typeof(EWeaponCategory), p_groupIndex))
                    {
                        return null;
                    }

                    return new WeaponInventorySlot(_nextSlotIndex++, (EWeaponCategory)p_groupIndex);

                case EItemType.Armor:
                    if (!Enum.IsDefined(typeof(EArmorType), p_groupIndex))
                    {
                        return null;
                    }

                    return new ArmorInventorySlot(_nextSlotIndex++, (EArmorType)p_groupIndex);

                case EItemType.Consumable:
                    if (!Enum.IsDefined(typeof(EConsumableType), p_groupIndex))
                    {
                        return null;
                    }

                    return new ConsumableInventorySlot(_nextSlotIndex++, (EConsumableType)p_groupIndex);

                case EItemType.Material:
                    if (!Enum.IsDefined(typeof(EMaterialType), p_groupIndex))
                    {
                        return null;
                    }

                    return new MaterialInventorySlot(_nextSlotIndex++, (EMaterialType)p_groupIndex);

                case EItemType.QuestItem:
                    // QuestItem은 ScrollView가 하나이므로 0만 사용한다.
                    if (p_groupIndex != 0)
                        return null;

                    return new CommonInventorySlot(_nextSlotIndex++, p_itemType);

                default:
                    return null;
            }
        }

        // 초기 셋팅
        private void CreateWeaponSlots()
        {
            AddWeaponSlots(EWeaponCategory.Melee, _weaponSlotCount);
            AddWeaponSlots(EWeaponCategory.Range, _weaponSlotCount);
            AddWeaponSlots(EWeaponCategory.Special, _weaponSlotCount);
        }

        // AddWeaponSlots 대상을 가능한 범위만큼 추가한다.
        private void AddWeaponSlots(EWeaponCategory p_weaponCategory, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new WeaponInventorySlot(_nextSlotIndex++, p_weaponCategory);
                _context.AddSlot(EItemType.Weapon, slot);   // 타입 별로 그룹화되어 InventoryContext에 추가된다.
            }
        }

        // 방어구 부위별 기본 슬롯을 생성한다.
        private void CreateArmorSlots()
        {
            AddArmorSlots(EArmorType.Helmet, _armorSlotCount);
            AddArmorSlots(EArmorType.Chest, _armorSlotCount);
            AddArmorSlots(EArmorType.Gloves, _armorSlotCount);
            AddArmorSlots(EArmorType.Boots, _armorSlotCount);
        }

        // 지정 방어구 부위에 슬롯을 추가한다.
        private void AddArmorSlots(EArmorType p_armorType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new ArmorInventorySlot(_nextSlotIndex++, p_armorType);
                _context.AddSlot(EItemType.Armor, slot);
            }
        }

        // 소비 아이템 종류별 기본 슬롯을 생성한다.
        private void CreateConsumableSlots()
        {
            AddConsumableSlots(EConsumableType.Heal, _commonSlotCount);
            AddConsumableSlots(EConsumableType.Mana, _commonSlotCount);
            AddConsumableSlots(EConsumableType.Pack, _commonSlotCount);
        }

        private void AddConsumableSlots(EConsumableType p_consumableType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = 
                    new ConsumableInventorySlot(_nextSlotIndex++, p_consumableType);

                _context.AddSlot(EItemType.Consumable, slot);
            }
        }

        // 재료 아이템 종류별 기본 슬롯을 생성한다.
        private void CreateMaterialSlots()
        {
            AddMaterialSlots(EMaterialType.Mineral, _commonSlotCount);
            AddMaterialSlots(EMaterialType.Organic, _commonSlotCount);
            AddMaterialSlots(EMaterialType.Essence, _commonSlotCount);
        }

        private void AddMaterialSlots(EMaterialType p_materialType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = 
                    new MaterialInventorySlot(_nextSlotIndex++, p_materialType);

                _context.AddSlot(EItemType.Material, slot);
            }
        }

        // 세부 분류가 없는 QuestItem 공용 슬롯을 생성한다.
        private void CreateQuestItemSlots()
        {
            for (int i = 0; i < _commonSlotCount; i++)
            {
                InventorySlot slot = 
                    new CommonInventorySlot(_nextSlotIndex++, EItemType.QuestItem);

                _context.AddSlot(EItemType.QuestItem, slot);
            }
        }
    }
}
