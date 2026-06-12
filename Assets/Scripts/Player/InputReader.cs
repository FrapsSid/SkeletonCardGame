using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, PlayerControls.IPlayerActions
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool JumpPressed { get; private set; }

    private PlayerControls _controls;

    private void Awake()
    {
        _controls = new PlayerControls();
        _controls.Player.SetCallbacks(this);
    }

    private void OnEnable()  => _controls.Player.Enable();
    private void OnDisable() => _controls.Player.Disable();

    public void OnMove(InputAction.CallbackContext ctx) =>
        MoveInput = ctx.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext ctx) =>
        LookInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) JumpPressed = true;
    }

    public void OnRun(InputAction.CallbackContext ctx) =>
        IsRunning = ctx.ReadValueAsButton();

    public void ConsumeJump() => JumpPressed = false;
}