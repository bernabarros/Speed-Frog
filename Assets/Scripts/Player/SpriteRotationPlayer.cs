using UnityEngine;

public class SpriteRotationPlayer : MonoBehaviour
{
    private SpriteRenderer _sR;
    private bool canFlip = true;
    private float previousAngle = 0f;

    private void Awake()
    {
        _sR = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
    }

    public void RotateSprite(float angle)
    {
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        transform.rotation = Quaternion.Euler(0f, 0f, snappedAngle);
    }

    public void FlipSprite(float horizontalInput, bool canFlip = true)
    {
        if (!canFlip) return;
        if (horizontalInput > 0.1f)
        {
            _sR.flipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            _sR.flipX = true;
        }
    }
}
