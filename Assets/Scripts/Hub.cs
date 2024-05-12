using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hub : MonoBehaviour
{
    private void Start()
    {

    }

    // Load level
    public void StaticLoadLevel(string levelName)
    {
        if (!LevelManager.Instance) return;

        LevelManager.Instance.LoadLevel(levelName);
        UI.Instance.ChangeScene("Game");
    }
}
