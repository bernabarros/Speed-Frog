using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExitGame : MonoBehaviour
{
    [SerializeField] private InputActionReference exitAction;

    // Update is called once per frame
    void Update()
    {
        if(exitAction.action.triggered)
        {
            
            #if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
            #else
                Application.Quit();
            #endif
        }
    }
}
