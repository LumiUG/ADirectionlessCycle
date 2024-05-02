using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public static GameManager Instance;

    void Awake()
    {
        // Singleton (GameManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // Pause event
    private void OnPause()
    {
        if (IsBadScene() || LevelManager.Instance.hasWon) return;
        LevelManager.Instance.PauseResumeGame(true);
    }

    // Returns if the current scene shouldn't be taken into account
    public bool IsBadScene()
    {
        return SceneManager.GetActiveScene().name == "Level Editor" || SceneManager.GetActiveScene().name == "Main Menu";
    }

    // Normalize direction vector
    public Vector3Int NormalizeVector(Vector3Int vector)
    {
        Vector3Int newVector = vector;
        newVector.Clamp(new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1));
        return newVector;
    }
}
