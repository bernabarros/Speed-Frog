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
    [SerializeField, Range(0f, 1f)] private float _unattachedVisualScale = 0.25f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private RotativeMovement movement;
    private bool isGrappling;
    private bool isAttached;
    private bool isGrapplePoint;
    private bool isAiming;
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
            // se o collider que o raycast colide for um trigger,
            // adiciona velocidade ao jogador para ele não parar no objeto
            if (isGrapplePoint)
            {
                rb.linearVelocity = _launchDirection * _grapplePointLaunchSpeed;
            }
            else
            {
                isAttached = true;
            }

            isGrapplePoint = false;
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

        // cria um raycast no final da lingua que pega
        // num collider caso tenha tag grapple

        RaycastHit2D hit = default;
        bool foundTarget = false;

        for (int i = 0; i < hitCount && !foundTarget; i++)
        {
            Transform target = hitResults[i].collider.transform;
            if (!target.IsChildOf(transform) && HasGrapplingTag(target))
            {
                hit = hitResults[i];
                foundTarget = true;
            }
        }

        if (!foundTarget)
        {
            return;
        }

        isGrapplePoint = hit.collider.isTrigger;
        _launchDirection = direction;
        _anchor = hit.point;
        _surfaceNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -direction;
        if (!isGrapplePoint)
        {
            movement.AttachToSurface(_surfaceNormal);
        }
        surfaceOffset = GetSurfaceOffset(_surfaceNormal);
        isGrappling = true;
        isAiming = false;
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
        bool showLine = isAiming || isGrappling || isAttached;
        _lR.enabled = showLine;
        if (!showLine)
        {
            return;
        }

        Vector3 start = _lR.transform.position;
        Vector2 end = isGrappling || isAttached
            ? _anchor
            : (Vector2)start + _aimDirection * (_length * _unattachedVisualScale);

        _lR.SetPosition(0, start);
        _lR.SetPosition(1, new Vector3(end.x, end.y, start.z));
    }
}