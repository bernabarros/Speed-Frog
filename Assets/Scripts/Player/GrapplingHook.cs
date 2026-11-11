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

    private Rigidbody2D _rb;
    private Collider2D _playerCollider;
    private RotativeMovement _movement;
    private bool _isGrappling;
    private bool _isAttached;
    private bool _isGrapplePoint;
    private bool _isAiming;
    private Vector2 _anchor;
    private Vector2 _surfaceNormal;
    private Vector2 _launchDirection;
    private Vector2 _aimDirection = Vector2.right;
    private float _surfaceOffset;
    private readonly List<RaycastHit2D> _hitResults = new List<RaycastHit2D>();

    public bool IsPulling => _isGrappling;
    public bool IsAttached => _isAttached;

    private Vector2 TargetPosition => _anchor + _surfaceNormal * _surfaceOffset;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerCollider = GetComponent<Collider2D>();
        _movement = GetComponent<RotativeMovement>();

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
        if (_isAttached)
        {
            _rb.position = TargetPosition;
            StopBody();
            return;
        }

        if (!_isGrappling)
        {
            return;
        }

        Vector2 toTarget = TargetPosition - _rb.position;
        float distance = toTarget.magnitude;

        StopBody();

        if (distance <= _stopDistance)
        {
            _rb.position = TargetPosition;
            _isGrappling = false;

            if (_isGrapplePoint)
            {
                _rb.linearVelocity = (_launchDirection + Vector2.up) * _grapplePointLaunchSpeed;
            }
            else
            {
                _isAttached = true;
                _movement.AttachToSurface(_surfaceNormal);
            }

            return;
        }

        float step = Mathf.Min(_pullSpeed * Time.fixedDeltaTime, distance);
        _rb.MovePosition(_rb.position + toTarget / distance * step);
    }

    public void SetAimState(Vector2 direction, bool isAiming)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            _aimDirection = direction.normalized;
        }

        _isAiming = isAiming && !_isGrappling && !_isAttached;
    }

    public void UseGrapple(Vector2 direction)
    {
        if (_isGrappling || _isAttached || direction.sqrMagnitude <= 0.0001f)
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
        _hitResults.Clear();
        int hitCount = Physics2D.Raycast(_lR.transform.position, direction, filter, _hitResults, _length);

        RaycastHit2D hit = default;
        bool foundTarget = false;

        for (int i = 0; i < hitCount && !foundTarget; i++)
        {
            Transform target = _hitResults[i].collider.transform;
            if (!target.IsChildOf(transform) && HasGrapplingTag(target))
            {
                hit = _hitResults[i];
                foundTarget = true;
            }
        }

        if (!foundTarget)
        {
            return;
        }

        _isGrapplePoint = hit.collider.isTrigger;
        _anchor = hit.point;
        _surfaceNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -direction;
        if (!_isGrapplePoint)
        {
            RotateImmediatelyToSurface(_surfaceNormal);
        }
        _surfaceOffset = GetSurfaceOffset(_surfaceNormal);
        _launchDirection = direction;
        _isGrappling = true;
        _isAiming = false;
        StopBody();
    }

    public void DetachForJump()
    {
        _isAttached = false;
    }

    private void StopBody()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private void RotateImmediatelyToSurface(Vector2 normal)
    {
        _rb.rotation = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Physics2D.SyncTransforms();
    }

    private float GetSurfaceOffset(Vector2 normal)
    {
        Bounds bounds = _playerCollider.bounds;
        return Mathf.Abs(normal.x) * bounds.extents.x + Mathf.Abs(normal.y) * bounds.extents.y;
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
        bool showLine = _isAiming || _isGrappling || _isAttached;
        _lR.enabled = showLine;
        if (!showLine)
        {
            return;
        }

        Vector3 start = _lR.transform.position;
        Vector2 end = _isGrappling || _isAttached
            ? _anchor
            : (Vector2)start + _aimDirection * (_length * _unattachedVisualScale);

        _lR.SetPosition(0, start);
        _lR.SetPosition(1, new Vector3(end.x, end.y, start.z));
    }
}