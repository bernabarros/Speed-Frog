using UnityEngine;

public class AlwaysRotate : MonoBehaviour
{
    [SerializeField] private float _speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, _speed * Time.deltaTime, 0f);
    }
}
