using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [HideInInspector] public static TransitionManager Instance;
    [HideInInspector] public Animator animator;

    internal enum Transitions { Crossfade, Reveal, Swipe };

    private AnimatorOverrideController crossfade;
    private AnimatorOverrideController reveal;
    private AnimatorOverrideController swipe;

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
        crossfade = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Crossfade/Base");
        reveal = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Reveal/Base");
        swipe = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Swipe/Base");

        // Play default
        ChangeTransition(Transitions.Swipe);
    }

    // Transitions out of a black screen
    private void SceneLoad(Scene scene, LoadSceneMode sceneMode) { TransitionOut(); }

    // Change the current animator controller
    internal void ChangeTransition(Transitions transition)
    {
        switch (transition)
        {
            case Transitions.Crossfade:
                animator.runtimeAnimatorController = crossfade;
                break;
            case Transitions.Reveal:
                animator.runtimeAnimatorController = reveal;
                break;
            case Transitions.Swipe:
                animator.runtimeAnimatorController = swipe;
                break;
        }
    }

    // Plays the transition IN and OUT
    public void TransitionIn() { animator.Play("IN"); }
    public void TransitionOut() { animator.Play("OUT"); }
}
