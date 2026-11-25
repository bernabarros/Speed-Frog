using System;
using UnityEngine;

public class Switch : MonoBehaviour
{
    [SerializeField] private bool switchStatus = false;
    private SpriteRenderer spriteRenderer;
    public bool SwitchStatus => switchStatus;
    public event Action OnSwitchActivated;
    [SerializeField] private Color onColor = Color.lightBlue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Activate()
    {
        if(switchStatus)
        {
            return;
        }
        
        switchStatus = true;
        spriteRenderer.color = onColor;

        OnSwitchActivated?.Invoke();
    }
    
}
