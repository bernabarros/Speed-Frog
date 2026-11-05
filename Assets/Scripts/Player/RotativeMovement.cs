using UnityEngine;

public class RotativeMovement : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private float _movementSpeed = 7f;
    [SerializeField] private float _accelerationRate = 20f;
    [SerializeField] private float _jumpForce = 7f; // (lol)

    [SerializeField] private float _gravityStrength = 9.81f;
    [SerializeField, Range(0f, 1f)] private float _surfaceGravityMovementScale = 0.25f;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private float _groundProbeOffset = 0.3f;
    [SerializeField] private float _groundSnapForce = 3f;
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private float _deaccelerationRate = 0.1f;

    private float _horizontalInput;
    private Vector2 _groundNormal;
    private Vector2 _startingGravityDirection;
    private float _startingRotation;
    private bool _isJumping;
    private bool _isGrounded;
    private bool _isIdle;
    private bool _isOnWallOrSlope;

    private void Awake()
    {
        if (input == null)
        {
            input = GetComponent<PlayerInputHandler>();
        }

        _groundNormal = transform.up; // direção de cima inicial
        _startingGravityDirection = Physics2D.gravity.normalized; // o mesmo só que para gravidade
        _startingRotation = _rigidbody2D.rotation;
    }

    private void Update()
    {
        _horizontalInput = input.GetMovementInput().x;
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

        bool gravityPullsAwayFromSurface = Vector2.Dot(_startingGravityDirection, groundNormal) > 0f; // verifica se o player está a ser empurrado para fora da superfície
        // e assim calcula o produto escalar entre as gravidades

        _isIdle = Mathf.Approximately(_horizontalInput, 0f);
        _isGrounded = touchingGround && !_isJumping && !(_isIdle && gravityPullsAwayFromSurface);

        if (_isGrounded) _groundNormal = groundNormal;

        RotateToGround(_isGrounded);

        if (_horizontalInput != 0f)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            Vector2 tangent = transform.right;
            float currentSpeed = Vector2.Dot(velocity, tangent);
            float targetSpeed = _horizontalInput * _movementSpeed;
            float gravityAlongTangent = _isGrounded ? Vector2.Dot(GetGroundGravity(groundNormal), tangent)
                : Vector2.Dot(_startingGravityDirection * _gravityStrength, tangent);
            float gravityOpposingInput = Mathf.Max(
                0f,
                -Mathf.Sign(_horizontalInput) * gravityAlongTangent);
            _isOnWallOrSlope = _isGrounded && !gravityPullsAwayFromSurface && Vector2.Dot(groundNormal, -_startingGravityDirection) < 0.999f;

            float acceleration = _isOnWallOrSlope ? Mathf.Max(0f, _accelerationRate - gravityOpposingInput) : _accelerationRate;

            float acceleratedSpeed = currentSpeed;

            if (currentSpeed * Mathf.Sign(_horizontalInput) < _movementSpeed)
            {
                acceleratedSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
            }

            Vector2 up = transform.up;
            Vector2 normalVelocity = up * Vector2.Dot(velocity, up);
            _rigidbody2D.linearVelocity = tangent * acceleratedSpeed + normalVelocity; // velocidade consoante a superficie
        }

        ApplyGravity(_isGrounded, _isIdle, gravityPullsAwayFromSurface);

        if (_isGrounded)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            float normalSpeed = Vector2.Dot(velocity, groundNormal);
            if (normalSpeed > 0f)
                _rigidbody2D.linearVelocity = velocity - groundNormal * normalSpeed;

            _rigidbody2D.AddForce(-groundNormal * _groundSnapForce, ForceMode2D.Force);
        }

        bool isFlatGround = Vector2.Dot(groundNormal, -_startingGravityDirection) >= 0.999f; // verifica se é chão

        if (_isGrounded && _isIdle && isFlatGround)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;

            Vector2 tangent = transform.right;

            float tangentSpeed = Vector2.Dot(velocity, tangent);

            float deceleration = _movementSpeed * _deaccelerationRate;

            float reducedTangentSpeed = Mathf.MoveTowards(tangentSpeed, 0f, deceleration * Time.fixedDeltaTime);

            _rigidbody2D.linearVelocity += tangent * (reducedTangentSpeed - tangentSpeed);
        }
    }

    // private bool IsGroundOrSlope(Vector2 groundNormal)
    // {
    //     return Vector2.Dot(groundNormal, -_startingGravityDirection) > 0f;
    // }

    private void RotateToGround(bool isGrounded) // rotação cosoante a superficie
    {
        float targetAngle = isGrounded
            ? Mathf.Atan2(_groundNormal.y, _groundNormal.x) * Mathf.Rad2Deg - 90f : _startingRotation; // tangente da superficie em graus - 90 graus = angulo 

        float angle = Mathf.MoveTowardsAngle(_rigidbody2D.rotation, targetAngle, _rotationSpeed * Time.fixedDeltaTime);

        _rigidbody2D.MoveRotation(angle);
    }

    private void ApplyGravity(bool isGrounded, bool isIdle, bool gravityPullsAwayFromSurface)
    {
        bool applyGravity = !isGrounded || !gravityPullsAwayFromSurface || isIdle;

        if (applyGravity)
        {
            Vector2 gravity = _startingGravityDirection * _gravityStrength;
            if (isGrounded && !isIdle)
                gravity = GetGroundGravity(_groundNormal);

            _rigidbody2D.AddForce(gravity, ForceMode2D.Force);
        }
    }

    private Vector2 GetGroundGravity(Vector2 groundNormal) // calcula quanto a gravidade é aplicada na velo consoante a superficie
    {
        Vector2 gravity = _startingGravityDirection * _gravityStrength;

        Vector2 intoSurface = groundNormal * Vector2.Dot(gravity, groundNormal);

        Vector2 alongSurface = gravity - intoSurface;

        return intoSurface + alongSurface * _surfaceGravityMovementScale;
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

    private bool TryGetGroundHit(Vector2 origin, Vector2 gravityDirection, out RaycastHit2D hit) // helper para trygetgroundnormal
    {
        hit = Physics2D.Raycast(origin, gravityDirection, _groundCheckDistance, _groundLayer);
        return hit && Vector2.Dot(hit.normal, -gravityDirection) > 0f;
    }

    private Vector2 GetRotatedGravityDirection() // método para receber a direção da gravidade rodada, para que o player se mova consoante a superfície
    {
        float rotationDelta = _rigidbody2D.rotation - _startingRotation;
        return Quaternion.Euler(0f, 0f, rotationDelta) * _startingGravityDirection;
    }

    public void Jump()
    {
        if (_isJumping || TryGetGroundNormal(out Vector2 groundNormal) != true)
            return;

        _isJumping = true;
        _rigidbody2D.AddForce(groundNormal * _jumpForce, ForceMode2D.Impulse);
    }
}
