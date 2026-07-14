
public class InventoryPresenter
{
    private readonly InventoryView _inventoryView;
    private readonly WeaponInventoryPresenter _weaponPresenter;
    private readonly ArmorInventoryPresenter _armorPresenter;
    private readonly ItemInventoryPresenter _consumablePresenter;
    private readonly ItemInventoryPresenter _materialPresenter;
    private readonly ItemInventoryPresenter _questPresenter;

    public InventoryPresenter
        (PlayerInventoryModule p_playerInventory, 
        InventoryView p_inventoryView, 
        ResourceLoadSystem p_resourceLoader,
        SlotTransferSystem p_transferSystem)
    {
        _inventoryView = p_inventoryView;

        _weaponPresenter = 
            new WeaponInventoryPresenter(p_playerInventory.WeaponInventory, p_inventoryView.WeaponView, p_resourceLoader, p_transferSystem);

        _armorPresenter = 
            new ArmorInventoryPresenter(p_playerInventory.ArmorInventory, p_inventoryView.ArmorView, p_resourceLoader, p_transferSystem);

        _consumablePresenter = 
            new ItemInventoryPresenter(p_playerInventory.ConsumableInventory, p_inventoryView.ConsumableView, p_resourceLoader, p_transferSystem);

        _materialPresenter = 
            new ItemInventoryPresenter(p_playerInventory.MaterialInventory, p_inventoryView.MaterialView, p_resourceLoader, p_transferSystem);

        _questPresenter = 
            new ItemInventoryPresenter(p_playerInventory.QuestInventory, p_inventoryView.QuestView, p_resourceLoader, p_transferSystem);

        p_playerInventory.InventoryChanged += Refresh;
    }

    public void Initialize()
    {
        _weaponPresenter.Initialize();
        _armorPresenter.Initialize();
        _consumablePresenter.Initialize();
        _materialPresenter.Initialize();
        _questPresenter.Initialize();
    }

    public bool ToggleWindow()
    {
        if (_inventoryView.IsOpen)
        {
            _inventoryView.CloseView();
        }
        else
        {
            Refresh();
            _inventoryView.OpenView((int)EInventoryView.Category);
        }

        return _inventoryView.IsOpen;
    }

    public void Refresh()
    {
        _weaponPresenter.Refresh();
        _armorPresenter.Refresh();
        _consumablePresenter.Refresh();
        _materialPresenter.Refresh();
        _questPresenter.Refresh();
    }
}
