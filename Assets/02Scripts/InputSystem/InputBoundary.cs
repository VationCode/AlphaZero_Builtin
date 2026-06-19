using UnityEngine;

public class InputBoundary : MonoBehaviour
{
    private InputSystem_Actions action;

    public Vector3 MoveInput => _moveInput;
    private Vector3 _moveInput;
    private void OnEnable()
    {
        action = new InputSystem_Actions();

        action.Player.Move.performed += i => _moveInput = i.ReadValue<Vector2>();

        // 활성화해야 동작
        action.Enable();
    }
}
