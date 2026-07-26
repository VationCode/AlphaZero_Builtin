using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.Player.Equipment;
using Alpha.Player.Inventory;
using Alpha.UI;
using Alpha.UI.Equipment;
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
    [SerializeField] private ItemDatabaseManager _itemDatabase;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    private PlayerInventoryPresenter _inventoryPresenter;
    private DamageSystem _damageSystem;
    private void Awake()
    {
        _damageSystem = new DamageSystem();
        // 외부 의존성만 Player에 연결한다.
        _playerCore.Bind(_input,_ui,_cameraCore,_resourceLoader,_damageSystem,_mouseSystem, _itemDatabase);
    }

    private async void Start()
    {
        await _itemDatabase.InitializeAsync();

        _cameraCore.Bind(_input, _playerCore.transform, _mouseSystem);
        _mouseSystem.Bind(_cameraCore.RenderCamera);
        _playerCore.InventoryFlow.Bind(_playerCore);

        // 인벤토리 창
        InventoryWindowPresenter();

        InventoryPresenter();
    }

    private void InventoryWindowPresenter()
    {
        _playerCore.InventoryFlow.OnWindowActivate += _ui.InventoryView.SetWindowActive;
        _ui.InventoryView.OnRequestWindowActive += _playerCore.InventoryFlow.SetWindowActive;
        _playerCore.InventoryFlow.OnWindowActivate += _mouseSystem.SetUICursor;
    }

    private void InventoryPresenter()
    {
        _inventoryPresenter = new PlayerInventoryPresenter(_playerCore.InventoryModule, _ui.InventoryView);

        _inventoryPresenter.Initialize();
    }

}
