using UnityEngine;
using UnityEngine.EventSystems;

public class Hub : MonoBehaviour
{
    public GameObject backButton;
    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(backButton);
    }

    // Load level
    public void StaticLoadLevel(string levelName)
    {
        if (!LevelManager.Instance) return;

        LevelManager.Instance.LoadLevel(levelName);
        UI.Instance.ChangeScene("Game");
    }
}
