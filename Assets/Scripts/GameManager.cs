using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public static GameManager Instance;
    private readonly string[] badScenes = { "Main Menu", "Level Editor", "Settings" };

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

    // DEBUG, load event
    private void OnDebugLoad()
    {
        if (IsBadScene()) return;
        LevelManager.Instance.LoadLevel($"1-{Random.Range(1,4)}");
    }

    // Returns if the current scene shouldn't be taken into account
    public bool IsBadScene()
    {
        return badScenes.Contains(SceneManager.GetActiveScene().name);
    }

    // Normalize direction vector
    public Vector3Int NormalizeVector(Vector3Int vector)
    {
        Vector3Int newVector = vector;
        newVector.Clamp(new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1));
        return newVector;
    }
}
