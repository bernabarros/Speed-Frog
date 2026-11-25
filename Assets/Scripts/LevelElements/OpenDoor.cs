using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] private Switch connectedSwitch;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        if (connectedSwitch != null)
        {
            connectedSwitch.OnSwitchActivated += Open;
        }
    }

    private void OnDisable()
    {
        if (connectedSwitch != null)
        {
            connectedSwitch.OnSwitchActivated -= Open;
        }
    }

    private void Open()
    {
        spriteRenderer.enabled = false;
        boxCollider2D.enabled = false;
    }
}
