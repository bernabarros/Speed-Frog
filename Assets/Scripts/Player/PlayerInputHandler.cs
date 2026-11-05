using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _movementInput;
    [SerializeField] private InputActionReference _jumpAction;

    [SerializeField] private RotativeMovement _rotativeMovement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_rotativeMovement == null)
        {
            _rotativeMovement = GetComponent<RotativeMovement>();
        }
    }

    private void Update()
    {
        if (_jumpAction.action.triggered)
        {
            _rotativeMovement.Jump();
        }
    }

    public Vector2 GetMovementInput()
    {
        return _movementInput.action.ReadValue<Vector2>();
    }
}
