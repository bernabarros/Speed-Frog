using UnityEngine;
using UnityEngine.InputSystem;

public class RotativeMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference _movementInput;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private float _movementSpeed = 7f;
    [SerializeField] private float _jumpForce = 7f; // (lol)

    [SerializeField] private float _gravityStrength = 9.81f;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private float _groundProbeOffset = 0.3f;
    [SerializeField] private float _groundSnapForce = 20f;
    [SerializeField] private float _rotationSpeed = 360f;

    private float _horizontalInput;
    private Vector2 _groundNormal;
    private Vector2 _startingGravityDirection;
    private float _startingRotation;
    private InputAction _jumpAction;
    private bool _isJumping;

    private void Awake()
    {
        _groundNormal = transform.up; // direção de cima inicial
        _startingGravityDirection = Physics2D.gravity.normalized; // o mesmo só que para gravidade
        _startingRotation = _rigidbody2D.rotation;
        _jumpAction = _movementInput.action.actionMap.FindAction("Jump");
    }

    private void OnEnable()
    {
        _jumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        _jumpAction.performed -= OnJump;
    }

    private void Update()
    {
        _horizontalInput = _movementInput.action.ReadValue<Vector2>().x;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        bool touchingGround = TryGetGroundNormal(out Vector2 groundNormal); // bool para verificar se toca numa superficie
        if (_isJumping && touchingGround && Vector2.Dot(_rigidbody2D.linearVelocity, groundNormal) <= 0f)
        {
            _isJumping = false;
        }

        bool isIdle = Mathf.Approximately(_horizontalInput, 0f); //nao toques no comando

        bool gravityPullsAwayFromSurface = Vector2.Dot(_startingGravityDirection, groundNormal) > 0f; // verifica se o player está a ser empurrado para fora da superfície
        // e assim calcula o produto escalar entre as gravidades

        bool isGrounded = touchingGround && !_isJumping && !(isIdle && gravityPullsAwayFromSurface);

        if (isGrounded) _groundNormal = groundNormal;

        RotateToGround(isGrounded);

        if (_horizontalInput != 0f)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            Vector2 up = transform.up;
            Vector2 tangent = transform.right;
            Vector2 normalVelocity = up * Vector2.Dot(velocity, up);
            _rigidbody2D.linearVelocity = tangent * (_horizontalInput * _movementSpeed) + normalVelocity; // velocidade consoante a superficie
        }

        ApplyGravity(isGrounded, groundNormal, isIdle); // gravidade consoante a superficie

        if (isGrounded && !isIdle)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            float normalSpeed = Vector2.Dot(velocity, groundNormal);
            if (normalSpeed > 0f)
                _rigidbody2D.linearVelocity = velocity - groundNormal * normalSpeed;

            _rigidbody2D.AddForce(-groundNormal * _groundSnapForce, ForceMode2D.Force);
        }
    }

    private void RotateToGround(bool isGrounded) // rotação cosoante a superficie
    {
        float targetAngle = isGrounded
            ? Mathf.Atan2(_groundNormal.y, _groundNormal.x) * Mathf.Rad2Deg - 90f : _startingRotation; // tangente da superficie em graus - 90 graus = angulo 

        float angle = Mathf.MoveTowardsAngle(_rigidbody2D.rotation, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
        _rigidbody2D.MoveRotation(angle);
    }

    private void ApplyGravity(bool isGrounded, Vector2 groundNormal, bool isIdle)
    {
        if (!isGrounded || isIdle)
        {
            _rigidbody2D.AddForce(_startingGravityDirection * _gravityStrength, ForceMode2D.Force);
            return;
        }

        Vector2 downhillDirection = _startingGravityDirection - groundNormal * Vector2.Dot(_startingGravityDirection, groundNormal); // é a actual direção que coincide com a de baixo original
        _rigidbody2D.AddForce(downhillDirection * _gravityStrength, ForceMode2D.Force);
    }

    private bool TryGetGroundNormal(out Vector2 groundNormal) // tenta obter o normal da superficie, e verifica se o player toca na parede
    {
        Vector2 gravityDirection = GetRotatedGravityDirection();
        Vector2 center = _groundCheck.position;
        Vector2 tangent = transform.right;

        if (TryGetGroundHit(center, gravityDirection, out RaycastHit2D hit))
        {
            groundNormal = hit.normal;
            return true;
        }

        bool leftGrounded = TryGetGroundHit(center - tangent * _groundProbeOffset, gravityDirection, out RaycastHit2D leftHit);

        bool rightGrounded = TryGetGroundHit(center + tangent * _groundProbeOffset, gravityDirection, out RaycastHit2D rightHit);

        if (leftGrounded && rightGrounded)
            hit = leftHit.distance <= rightHit.distance ? leftHit : rightHit;
        else if (leftGrounded)
            hit = leftHit;
        else if (rightGrounded)
            hit = rightHit;
        else
        {
            groundNormal = _groundNormal;
            return false;
        }

        groundNormal = hit.normal;
        return true;
    }

    private bool TryGetGroundHit(Vector2 origin, Vector2 gravityDirection, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(origin, gravityDirection, _groundCheckDistance, _groundLayer);
        return hit && Vector2.Dot(hit.normal, -gravityDirection) > 0f;
    }

    private Vector2 GetRotatedGravityDirection() // método para receber a direção da gravidade rodada, para que o player se mova consoante a superfície
    {
        float rotationDelta = _rigidbody2D.rotation - _startingRotation;
        return Quaternion.Euler(0f, 0f, rotationDelta) * _startingGravityDirection;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        Jump();
    }

    private void Jump()
    {
        if (_isJumping || TryGetGroundNormal(out Vector2 groundNormal) != true)
            return;

        _isJumping = true;
        _rigidbody2D.AddForce(groundNormal * _jumpForce, ForceMode2D.Impulse);
    }
}
