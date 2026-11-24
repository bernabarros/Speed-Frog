using System.Collections.Generic;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    private const string GrapplingTag = "Grappling";

    [SerializeField] private LineRenderer _lR;
    [SerializeField] private float _length = 5f;
    [SerializeField] private float _pullSpeed = 12f;
    [SerializeField] private float _stopDistance = 0.2f;
    [SerializeField] private float _grapplePointLaunchSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float _wallNormalTolerance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _unattachedVisualScale = 0.25f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private RotativeMovement movement;
    private GrappleColliderToggle colliderToggle;
    private bool isGrappling;
    private bool isAttached;
    private bool isLaunchTarget;
    private bool isAiming;
    private bool isShowingEnableTarget;
    private Vector2 _anchor;
    private Vector2 _surfaceNormal;
    private Vector2 _launchDirection;
    private Vector2 _aimDirection = Vector2.right;
    private float surfaceOffset;
    private readonly List<RaycastHit2D> hitResults = new List<RaycastHit2D>();

    public bool IsPulling => isGrappling;
    public bool IsAttached => isAttached;

    private Vector2 TargetPosition => _anchor + _surfaceNormal * surfaceOffset;
    [SerializeField] private float _surfaceSkin = 0.02f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        movement = GetComponent<RotativeMovement>();
        colliderToggle = GetComponent<GrappleColliderToggle>();

        _lR.useWorldSpace = true;
        _lR.positionCount = 2;
        _lR.enabled = false;
    }

    private void Update()
    {
        UpdateLine();
    }

    private void FixedUpdate()
    {
        if (isAttached) // so sai de uma superficie quando saltar, caso tenha usado lingua na parede
        {
            rb.position = TargetPosition;
            StopBody();
            return;
        }

        if (!isGrappling)
        {
            return;
        }

        Vector2 toTarget = TargetPosition - rb.position;
        float distance = toTarget.magnitude;

        StopBody();

        if (distance <= _stopDistance)
        {
            rb.position = TargetPosition;
            isGrappling = false;
            // se for trigger ou uma superficie que nao seja parede,
            // adiciona velocidade para o jogador continuar o movimento
            if (isLaunchTarget)
            {
                rb.linearVelocity = _launchDirection * _grapplePointLaunchSpeed;
            }
            else
            {
                isAttached = true;
            }

            isLaunchTarget = false;
            return;
        }

        float step = Mathf.Min(_pullSpeed * Time.fixedDeltaTime, distance);
        rb.MovePosition(rb.position + toTarget / distance * step);
    }

    public void SetAimState(Vector2 direction, bool aiming)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            _aimDirection = direction.normalized;
        }

        if (!aiming)
        {
            isShowingEnableTarget = false;
        }

        isAiming = aiming && !isGrappling && !isAttached;
    }

    public void UseGrapple(Vector2 direction)
    {
        if (isGrappling || isAttached || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();
        _aimDirection = direction;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };
        hitResults.Clear();
        int hitCount = Physics2D.Raycast(_lR.transform.position, direction, filter, hitResults, _length);

        // seleciona o objeto mais proximo atingido pelo raycast
        RaycastHit2D hit = default;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D candidate = hitResults[i];
            Transform target = candidate.collider.transform;
            if (target == transform || target.IsChildOf(transform))
            {
                continue;
            }

            bool isGrapplingTarget = HasGrapplingTag(target);
            bool isEnableTarget = colliderToggle.IsEnableTarget(target);
            if (candidate.collider.isTrigger && !isGrapplingTarget && !isEnableTarget)
            {
                continue;
            }

            if (candidate.distance < nearestDistance)
            {
                hit = candidate;
                nearestDistance = candidate.distance;
            }
        }

        if (nearestDistance == float.PositiveInfinity)
        {
            return;
        }

        colliderToggle.HandleGrappleHit(hit.collider);
        isShowingEnableTarget = colliderToggle.IsEnableTarget(hit.collider.transform);
        if (isShowingEnableTarget)
        {
            _anchor = hit.point;
        }

        // colliders sem a tag bloqueiam o grapple
        if (!HasGrapplingTag(hit.collider.transform))
        {
            return;
        }

        _launchDirection = direction;
        _anchor = hit.point;
        _surfaceNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -direction;
        isLaunchTarget = hit.collider.isTrigger || !IsWallSurface(_surfaceNormal);
        if (!isLaunchTarget)
        {
            movement.AttachToSurface(_surfaceNormal);
        }
        surfaceOffset = GetSurfaceOffset(_surfaceNormal);
        isGrappling = true;
        isAiming = false;
        isShowingEnableTarget = false;
        StopBody();
    }

    public void DetachForJump()
    {
        isAttached = false;
    }

    private void StopBody()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private float GetSurfaceOffset(Vector2 normal)
    {
        Bounds bounds = playerCollider.bounds;
        return Mathf.Abs(normal.x) * bounds.extents.x
             + Mathf.Abs(normal.y) * bounds.extents.y
             + _surfaceSkin;
    }

    private bool IsWallSurface(Vector2 normal)
    {
        return Mathf.Abs(Vector2.Dot(normal, Physics2D.gravity.normalized)) <= _wallNormalTolerance;
    }

    private static bool HasGrapplingTag(Transform target)
    {
        while (target != null)
        {
            if (target.CompareTag(GrapplingTag))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private void UpdateLine()
    {
        bool showLine = isAiming || isGrappling || isAttached || isShowingEnableTarget;
        _lR.enabled = showLine;
        if (!showLine)
        {
            return;
        }

        Vector3 start = _lR.transform.position;
        Vector2 end = isGrappling || isAttached || isShowingEnableTarget
            ? _anchor
            : (Vector2)start + _aimDirection * (_length * _unattachedVisualScale);

        _lR.SetPosition(0, start);
        _lR.SetPosition(1, new Vector3(end.x, end.y, start.z));
    }
}