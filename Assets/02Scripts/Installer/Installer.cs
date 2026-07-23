using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.Player.Inventory;
using Alpha.UI;
using Alpha.UI.Inventory;
using System.Collections.Generic;
using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private AlphaInputSystem _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _cameraCore;

    [Header("Resource")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    private DamageSystem _damageSystem;
    private void Awake()
    {
        _damageSystem = new DamageSystem();

        // 외부 의존성만 Player에 연결한다.
        _playerCore.Bind(_input,_ui,_cameraCore,_resourceLoader,_damageSystem,_mouseSystem);
    }

    private void Start()
    {
        _cameraCore.Bind(_input, _playerCore.transform, _mouseSystem);
        _mouseSystem.Bind(_cameraCore.RenderCamera);
        _playerCore.InventoryFlow.Bind(_playerCore);

        // 인벤토리 창
        InventoryWindowPresenter();

        // Slot
        SlotPresenter();
    }

    private void InventoryWindowPresenter()
    {
        _playerCore.InventoryFlow.OnWindowActivate += _ui.InventoryView.SetWindowActive;
        _ui.InventoryView.OnRequestWindowActive += _playerCore.InventoryFlow.SetWindowActive;
        _playerCore.InventoryFlow.OnWindowActivate += _mouseSystem.SetUICursor;
    }

    private void SlotPresenter()
    {
        PlayerInventoryFlow flow = _playerCore.InventoryFlow;

        BindSlotRequestEvents();

        flow.OnSlotsInitialized += HandleSlotsInitialized;
        flow.OnSlotsAdded += HandleSlotsAdded;

        // 논리 슬롯 초기화 요청
        flow.InitializeSlots();
    }
    #region ==================== 로직 동작 -> 동작 내용 View 연결
    // 초기 슬롯 목록 전달
    private void HandleSlotsInitialized()
    {
        PlayerInventoryModule module = _playerCore.InventoryModule;

        ConnectSlotGroup(module.GetWeaponSlots(EWeaponType.Melee));
        ConnectSlotGroup(module.GetWeaponSlots(EWeaponType.Range));
        ConnectSlotGroup(module.GetWeaponSlots(EWeaponType.Special));

        ConnectSlotGroup(module.GetArmorSlots(EArmorType.Helmet));
        ConnectSlotGroup(module.GetArmorSlots(EArmorType.Chest));
        ConnectSlotGroup(module.GetArmorSlots(EArmorType.Gloves));
        ConnectSlotGroup(module.GetArmorSlots(EArmorType.Boots));

        ConnectSlotGroup(module.GetCommonSlots(EItemType.Consumable));
        ConnectSlotGroup(module.GetCommonSlots(EItemType.Material));
        ConnectSlotGroup(module.GetCommonSlots(EItemType.QuestItem));
    }


    // 신규 슬롯 목록 전달
    // flow.OnSlotsAdded에 구독되어 있기에 flow의 각 AddSlot함수들에서 동작이됨
    private void HandleSlotsAdded(IReadOnlyList<SlotBase> p_slots)
    {
        ConnectSlotGroup(p_slots);
    }

    // 생성과 이벤트 연결
    private void ConnectSlotGroup(IReadOnlyList<SlotBase> p_slots)
    {
        if (p_slots == null || p_slots.Count == 0)
            return;

        if (!TryGetSlotViewTarget(p_slots[0], out InventoryPageView page, out int groupIndex))
        {
            return;
        }

        IReadOnlyList<SlotViewBase> slotViews = page.AddSlotView(groupIndex, p_slots.Count);

        if (slotViews.Count != p_slots.Count)
        {
            Debug.LogError("Slot과 SlotView 개수가 다릅니다.");
            return;
        }

        for (int i = 0; i < p_slots.Count; i++)
        {
            SlotBase slot = p_slots[i];
            SlotViewBase slotView = slotViews[i];

            slotView.Bind(_resourceLoader);
            slot.OnSlotChanged += slotView.SetSlot;

            // 현재 슬롯 상태 최초 반영
            slotView.SetSlot(slot.Item, slot.Count);
        }
    }
    // UI 위치 판단
    private bool TryGetSlotViewTarget(SlotBase p_slot, out InventoryPageView p_page, out int p_groupIndex)
    {
        EInventoryPage pageType;

        p_page = null;
        p_groupIndex = 0;

        switch (p_slot)
        {
            case WeaponSlot weaponSlot:
                pageType = EInventoryPage.Weapon;
                p_groupIndex = (int)weaponSlot.WeaponType;
                break;

            case ArmorSlot armorSlot:
                pageType = EInventoryPage.Armor;
                p_groupIndex = (int)armorSlot.ArmorType;
                break;

            case CommonSlot commonSlot:
                switch (commonSlot.ItemType)
                {
                    case EItemType.Consumable:
                        pageType = EInventoryPage.Consumable;
                        break;

                    case EItemType.Material:
                        pageType = EInventoryPage.Material;
                        break;

                    case EItemType.QuestItem:
                        pageType = EInventoryPage.Quest;
                        break;

                    default:
                        return false;
                }

                break;

            default:
                return false;
        }

        p_page = _ui.InventoryView.GetPage(pageType);
        return p_page != null;
    }
    #endregion ==================== /로직 동작 -> 동작 내용 View 연결

    #region ======================================== View에서의 요청 -> 로직 동작 -> 동작 내용 View 연결 
    // UI쪽에서 버튼으로 슬롯 생성 시에 대한 요청 연결
    // View 요청 -> 논리 생성 AddWeaponSlots안에 위에 HandleSlotsAdded가 동작됨 그럼 다시 View에 표현으로연결
    private void BindSlotRequestEvents()
    {
        PlayerInventoryView view = _ui.InventoryView;

        view.GetPage(EInventoryPage.Weapon).OnRequestAddSlot
            += HandleWeaponSlotRequest;

        view.GetPage(EInventoryPage.Armor).OnRequestAddSlot
            += HandleArmorSlotRequest;

        view.GetPage(EInventoryPage.Consumable).OnRequestAddSlot
            += HandleConsumableSlotRequest;

        view.GetPage(EInventoryPage.Material).OnRequestAddSlot
            += HandleMaterialSlotRequest;

        view.GetPage(EInventoryPage.Quest).OnRequestAddSlot
            += HandleQuestItemSlotRequest;
    }

    private void HandleWeaponSlotRequest(int p_groupIndex)
    {
        _playerCore.InventoryFlow.AddWeaponSlots((EWeaponType)p_groupIndex);
    }
    private void HandleArmorSlotRequest(int p_groupIndex)
    {
        _playerCore.InventoryFlow.AddArmorSlots((EArmorType)p_groupIndex);
    }

    // Common 핸들러는 타입을 직접 지정
    private void HandleConsumableSlotRequest(int p_groupIndex)
    {
        if (p_groupIndex != 0) return;

        _playerCore.InventoryFlow.AddCommonSlots(EItemType.Consumable);
    }
    private void HandleMaterialSlotRequest(int p_groupIndex)
    {
        if (p_groupIndex != 0) return;

        _playerCore.InventoryFlow.AddCommonSlots(EItemType.Material);
    }
    private void HandleQuestItemSlotRequest(int p_groupIndex)
    {
        if (p_groupIndex != 0) return;

        _playerCore.InventoryFlow.AddCommonSlots(EItemType.QuestItem);
    }
    #endregion ======================================== /View에서의 요청
}
