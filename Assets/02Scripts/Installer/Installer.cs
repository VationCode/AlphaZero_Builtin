using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private InputManager _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _camera;
    [SerializeField] private ItemDB _itemDB;

    private void Awake()
    {
        _player.Bind(_input, _ui, _camera, _itemDB);
    }

}
