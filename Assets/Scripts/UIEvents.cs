using UnityEngine;
using UnityEngine.SceneManagement;

public class UIEvents : MonoBehaviour
{
    // Change scenes.
    public void ChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Exit application.
    public void ExitApplication() { Application.Quit(); }
}
