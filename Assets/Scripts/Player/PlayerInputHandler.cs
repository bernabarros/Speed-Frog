using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _movementInput;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _grappleAction;

    [SerializeField] private RotativeMovement _rotativeMovement;
    [SerializeField] private GrapplingHook _grapplingHook;

    private Vector2 previousMovBeforeZero;

    void Start()
    {
        _rotativeMovement = GetComponent<RotativeMovement>();
        _grapplingHook = GetComponent<GrapplingHook>();
        previousMovBeforeZero = Vector2.right;
    }

    private void Update()
    {
        Vector2 movementInput = _movementInput.action.ReadValue<Vector2>();

        if (_jumpAction.action.triggered)
        {
            _rotativeMovement.Jump(movementInput);
        }

        if (movementInput.magnitude > 0.1f)
        {
            previousMovBeforeZero = movementInput;
        }

        bool grappleHeld = _grappleAction.action.IsPressed();
        _grapplingHook.SetAimState(previousMovBeforeZero, grappleHeld);

        if (_grappleAction.action.triggered)
        {
            _grapplingHook.UseGrapple(previousMovBeforeZero);
        }
    }

    public Vector2 GetMovementInput()
    {
        return _movementInput.action.ReadValue<Vector2>();
    }
}
