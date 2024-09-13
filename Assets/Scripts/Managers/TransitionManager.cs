using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [HideInInspector] public static TransitionManager Instance;

    [HideInInspector] public Animator animator;
    private AnimatorOverrideController crossfade;

    void Awake()
    {
        // Singleton (TransManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Add to sceneloaded event
        SceneManager.sceneLoaded += SceneLoad;

        // Get overrides & animator
        animator = GetComponent<Animator>();
        crossfade = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Crossfade");

        // Play default
        animator.Play("OUT");
    }

    // Transitions out of a black screen
    private void SceneLoad(Scene scene, LoadSceneMode sceneMode) { TransitionOut(); }

    public void TransitionIn() { animator.Play("IN"); }
    public void TransitionOut() { animator.Play("OUT"); }
}
