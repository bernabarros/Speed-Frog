using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelChange : MonoBehaviour
{
    [SerializeField] private int levelIndex;
    [SerializeField] private InputActionReference changeLevelAction;

    void Update()
    {
        if(changeLevelAction.action.triggered)
        {
            LoadLevel();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.GetComponent<PlayerInputHandler>() != null)
        {
            LoadLevel();
        }
    }

    private void LoadLevel()
    {
        SceneManager.LoadScene(levelIndex);
    }
}
