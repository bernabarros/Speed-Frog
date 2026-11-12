using UnityEngine;

public class RotativeMovement : MonoBehaviour
{
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
    [SerializeField] private float _returnToNormalSpeed = 90f;
    [SerializeField] private float _returnToNormalDelay = 0.25f;
    [SerializeField] private float _deaccelerationRate = 0.1f;

    private PlayerInputHandler input;
    private GrapplingHook grapplingHook;
    private SpriteRotationPlayer _sprRot;
    private float horizontalInput;
    private Vector2 groundNormalOG;
    private Vector2 startingGravityDirection;
    private float startingRotation;
    private bool isJumping;
    private bool isGrappleJumping;
    private bool isGrounded;
    private float oneSideProbeDuration;

    public LayerMask GroundLayer => _groundLayer;

    private float Flatness => Vector2.Dot(groundNormalOG, -startingGravityDirection);

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        grapplingHook = GetComponent<GrapplingHook>();
        _sprRot = GetComponentInChildren<SpriteRotationPlayer>();
        groundNormalOG = transform.up; // direção de cima inicial
        startingGravityDirection = Physics2D.gravity.normalized; // o mesmo só que para gravidade
        startingRotation = _rigidbody2D.rotation;
    }

    private void Update()
    {
        horizontalInput = input.GetMovementInput().x;
        _sprRot.RotateSprite(grapplingHook.IsAttached || isGrappleJumping ? GetGroundAngle() : _rigidbody2D.rotation);
        _sprRot.FlipSprite(horizontalInput, isGrounded);
    }

    private void FixedUpdate()
    {
        if (grapplingHook.IsPulling || grapplingHook.IsAttached)
        {
            return;
        }

        HandleMovement();
    }

    private void HandleMovement()
    {
        bool touchingGround = TryGetGroundNormal(out Vector2 groundNormal, out bool onlyOneSideProbeHit);
        if (isJumping && touchingGround && Vector2.Dot(_rigidbody2D.linearVelocity, groundNormal) <= 0f)
        {
            isJumping = false;
            isGrappleJumping = false;
        }

        bool gravityPullsAwayFromSurface = Vector2.Dot(startingGravityDirection, groundNormal) > 0f; // verifica se o player está a ser empurrado para fora da superfície
        // e assim calcula o produto escalar entre as gravidades

        bool isIdle = Mathf.Approximately(horizontalInput, 0f);
        isGrounded = touchingGround && !isJumping && !(isIdle && gravityPullsAwayFromSurface);

        if (isGrounded) groundNormalOG = groundNormal;

        if (onlyOneSideProbeHit)
        {
            oneSideProbeDuration += Time.fixedDeltaTime;
            if (oneSideProbeDuration >= _returnToNormalDelay)
                RotateToGround(false, _returnToNormalSpeed);
        }
        else
        {
            oneSideProbeDuration = 0f;
            RotateToGround(isGrounded, _rotationSpeed);
        }

        if (horizontalInput != 0f)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            Vector2 tangent = isGrounded ? GetSurfaceTangent(groundNormalOG) : transform.right;
            float currentSpeed = Vector2.Dot(velocity, tangent);
            float inputDirection = Mathf.Sign(horizontalInput);
            float acceleratedSpeed = currentSpeed;

            if (currentSpeed * inputDirection < _movementSpeed)
            {
                acceleratedSpeed += inputDirection * _accelerationRate * Time.fixedDeltaTime;
                if (acceleratedSpeed * inputDirection > _movementSpeed)
                    acceleratedSpeed = inputDirection * _movementSpeed;
            }

            Vector2 normal = isGrounded ? groundNormalOG : (Vector2)transform.up;
            Vector2 normalVelocity = normal * Vector2.Dot(velocity, normal);
            _rigidbody2D.linearVelocity = tangent * acceleratedSpeed + normalVelocity; // velocidade consoante a superficie
        }

        ApplyGravity(isGrounded, isIdle, gravityPullsAwayFromSurface);

        if (isGrounded)
        {
            if (Vector2.Dot(_rigidbody2D.linearVelocity, groundNormalOG) > 0f)
                SetVelocityAlong(groundNormalOG, 0f);

            _rigidbody2D.AddForce(-groundNormalOG * _groundSnapForce, ForceMode2D.Force);
        }

        bool isFlatGround = Flatness >= 0.001f; // verifica se é chão

        if (isGrounded && isIdle && isFlatGround)
        {
            Vector2 tangent = GetSurfaceTangent(groundNormalOG);
            float tangentSpeed = Vector2.Dot(_rigidbody2D.linearVelocity, tangent);
            float deceleration = _movementSpeed * _deaccelerationRate;

            SetVelocityAlong(tangent, Mathf.MoveTowards(tangentSpeed, 0f, deceleration * Time.fixedDeltaTime));
        }
    }

    private void SetVelocityAlong(Vector2 direction, float speed)
    {
        _rigidbody2D.linearVelocity += direction * (speed - Vector2.Dot(_rigidbody2D.linearVelocity, direction));
    }

    private void RotateToGround(bool isGrounded, float rotationSpeed) // rotação cosoante a superficie
    {
        float targetAngle = isGrounded ? GetGroundAngle() : startingRotation;

        _rigidbody2D.MoveRotation(Mathf.MoveTowardsAngle(_rigidbody2D.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime));
    }

    private float GetGroundAngle() => Mathf.Atan2(groundNormalOG.y, groundNormalOG.x) * Mathf.Rad2Deg - 90f;

    private void SnapSprite()
    {
        _sprRot.RotateSprite(GetGroundAngle());
        Physics2D.SyncTransforms();
    }

    public void AttachToSurface(Vector2 surfaceNormal)
    {
        groundNormalOG = surfaceNormal.normalized;
        isGrounded = true;
        isJumping = false;
        _rigidbody2D.rotation = GetGroundAngle();
        SnapSprite();
    }

    private void ApplyGravity(bool isGrounded, bool isIdle, bool gravityPullsAwayFromSurface)
    {
        if (isGrounded && gravityPullsAwayFromSurface && !isIdle)
            return;

        Vector2 gravity = startingGravityDirection * _gravityStrength;
        if (isGrounded && !isIdle)
        {
            gravity = GetGroundGravity(groundNormalOG);
            if (IsSlope())
                gravity = groundNormalOG * Vector2.Dot(gravity, groundNormalOG);
        }

        _rigidbody2D.AddForce(gravity, ForceMode2D.Force);
    }

    private bool IsSlope() => Flatness > 0.001f && Flatness < 0.999f;

    private Vector2 GetSurfaceTangent(Vector2 groundNormal)
    {
        Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
        return Vector2.Dot(tangent, transform.right) < 0f ? -tangent : tangent;
    }

    private Vector2 GetGroundGravity(Vector2 groundNormal) // calcula quanto a gravidade é aplicada na velo consoante a superficie
    {
        Vector2 gravity = startingGravityDirection * _gravityStrength;
        Vector2 intoSurface = groundNormal * Vector2.Dot(gravity, groundNormal);

        return intoSurface + (gravity - intoSurface) * _surfaceGravityMovementScale;
    }

    private bool TryGetGroundNormal(out Vector2 groundNormal, out bool onlyOneSideProbeHit) // tem 3 probes por baixo do jogador
    // um no centro os outros dois nos lados, se um dos lados estiver no chão e o do meio não,
    // rodar lentamente o player
    {
        Vector2 gravityDirection = GetRotatedGravityDirection();
        Vector2 center = _groundCheck.position;
        Vector2 side = transform.right * _groundProbeOffset;
        bool centerGrounded = TryGetGroundHit(center, gravityDirection, out RaycastHit2D centerHit);
        bool leftGrounded = TryGetGroundHit(center - side, gravityDirection, out RaycastHit2D leftHit);
        bool rightGrounded = TryGetGroundHit(center + side, gravityDirection, out RaycastHit2D rightHit);
        onlyOneSideProbeHit = !centerGrounded && leftGrounded != rightGrounded;

        if (centerGrounded)
        {
            groundNormal = centerHit.normal;
            return true;
        }

        if (!leftGrounded && !rightGrounded)
        {
            groundNormal = groundNormalOG;
            return false;
        }

        bool useLeft = leftGrounded && (!rightGrounded || leftHit.distance <= rightHit.distance);
        groundNormal = (useLeft ? leftHit : rightHit).normal;
        return true;
    }

    private bool TryGetGroundHit(Vector2 origin, Vector2 gravityDirection, out RaycastHit2D hit) // helper para trygetgroundnormal
    {
        hit = Physics2D.Raycast(origin, gravityDirection, _groundCheckDistance, _groundLayer);
        return hit && Vector2.Dot(hit.normal, -gravityDirection) > 0f;
    }

    private Vector2 GetRotatedGravityDirection() // método para receber a direção da gravidade rodada, para que o player se mova consoante a superfície
        => Quaternion.Euler(0f, 0f, _rigidbody2D.rotation - startingRotation) * startingGravityDirection;

    public void Jump(Vector2 movementInput)
    {
        if (grapplingHook.IsPulling)
        {
            return;
        }

        if (grapplingHook.IsAttached)
        {
            Vector2 attachedNormal = groundNormalOG;
            Vector2 jumpDir;
            if (Vector2.Dot(attachedNormal, Vector2.up) < 0.001f &&
                movementInput.x > 0.5f && movementInput.y > 0.1f ||
                Vector2.Dot(attachedNormal, Vector2.up) < 0.001f && movementInput.x < -0.5f && movementInput.y > 0.1f)
            {
                jumpDir = (Vector2.up+attachedNormal).normalized;
            }
            else if (Vector2.Dot(attachedNormal, Vector2.up) < 0.001f &&
                movementInput.x > 0.5f || Vector2.Dot(attachedNormal, Vector2.up) < 0.001f && movementInput.x < -0.5f)
            {
                jumpDir = attachedNormal;
            }
            else if (Vector2.Dot(attachedNormal, Vector2.up) < 0.001f &&
                movementInput.y < -0.1f)
            {
                jumpDir = Vector2.down;
            }
            else
            {
                jumpDir = Vector2.up;
            }

            grapplingHook.DetachForJump();
            _rigidbody2D.rotation = startingRotation;
            groundNormalOG = -startingGravityDirection;
            isGrappleJumping = true;
            isJumping = true;
            SnapSprite();
            _rigidbody2D.AddForce(jumpDir * _jumpForce, ForceMode2D.Impulse);
            return;
        }

        if (isJumping || !TryGetGroundNormal(out Vector2 groundNormal, out _))
        {
            return;
        }

        isJumping = true;
        _rigidbody2D.AddForce(groundNormal * _jumpForce, ForceMode2D.Impulse);
    }


    ///
    /// Gizmos
    /// 

    private void OnDrawGizmosSelected()
    {
        Vector2 gravityDirection = GetRotatedGravityDirection();
        if (gravityDirection == Vector2.zero)
            gravityDirection = Vector2.down;

        Vector2 center = _groundCheck.position;

        for (int i = -1; i <= 1; i++)
            DrawGroundProbe(center + (Vector2)transform.right * (i * _groundProbeOffset), gravityDirection);
    }

    private void DrawGroundProbe(Vector2 origin, Vector2 gravityDirection)
    {
        bool validHit = TryGetGroundHit(origin, gravityDirection, out RaycastHit2D hit);
        Vector2 end = hit.collider != null ? hit.point : origin + gravityDirection * _groundCheckDistance;

        Gizmos.color = validHit ? Color.green : Color.red;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(origin, 0.035f);
    }
}