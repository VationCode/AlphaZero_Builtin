using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private InputSystem_Alpha _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _camera;
    //[SerializeField] private ItemParse _itemDB;
    [SerializeField] private InventorySystem _inventory;

    private void Awake()
    {
        _player.Bind(_input, _ui, _camera, _inventory);
        _inventory.Bind();
    }
}
