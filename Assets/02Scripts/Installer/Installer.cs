using Alpha.AlphaCamera;
using Alpha.Equipment;
using Alpha.Inventory;
using Alpha.Mouse;
using Alpha.Player;
using Unity.VisualScripting;
using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _playerCore;
    [SerializeField] private AlphaInputSystem _input;
    [SerializeField] private CameraCore _cameraCore;

    [Header("Inventory")]
    [SerializeField] private InventoryCore _inventoryCore;


    [Header("Equipment")]
    [SerializeField] private EquipmentCore _equipmentCore;

    [Header("Resource")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;
    [SerializeField] private ItemDatabaseManager _itemDatabase;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    private DamageSystem _damageSystem;

    private void Awake()
    {
        _damageSystem = new DamageSystem();
        // 외부 의존성만 Player에 연결한다.
        _playerCore.Bind(_input,_cameraCore,_damageSystem,_mouseSystem);
    }

    private async void Start()
    {
        await _itemDatabase.InitializeAsync();

        _cameraCore.Bind(_input, _playerCore.transform, _mouseSystem);
        _mouseSystem.Bind(_cameraCore.RenderCamera);
        _playerCore.ItemPickupFlow.Bind(_inventoryCore, _itemDatabase);

        // UI Entity 내부 상태 초기화
        // Inventory Presenter가 먼저 생성되어야 한다.
        _inventoryCore.Bind(_input, _resourceLoader);
        _equipmentCore.Bind(_resourceLoader, _inventoryCore.Presenter);
        
        // Inventory 외부 연결
        _inventoryCore.Flow.OnWindowActivate += _mouseSystem.SetUICursor;
    }
}
