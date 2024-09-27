using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [HideInInspector] public static TransitionManager Instance;
    [HideInInspector] public Animator animator;

    internal bool inTransition = false;
    internal enum Transitions { Ignore, Crossfade, Reveal, Swipe, Triangle, Unknown };
    internal EventSystem eventReference;
    internal Coroutine currentTransition = null;

    private AnimatorOverrideController crossfade;
    private AnimatorOverrideController reveal;
    private AnimatorOverrideController swipe;
    private AnimatorOverrideController triangle;
    private AnimatorOverrideController unknown;

    void Awake()
    {
        // Singleton (TransManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Add to sceneloaded event
        SceneManager.sceneLoaded += SceneLoad;

        // Get overrides & animator
        eventReference = eventReference = EventSystem.current;
        animator = GetComponent<Animator>();
        crossfade = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Crossfade/Base");
        reveal = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Reveal/Base");
        swipe = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Swipe/Base");
        triangle = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Triangle/Base");
        unknown = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Unknown/Base");

        // Play default
        ChangeTransition(Transitions.Reveal);
    }

    // Transitions out of a black screen
    private void SceneLoad(Scene scene, LoadSceneMode sceneMode) { TransitionOut<string>(); }

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
            case Transitions.Triangle:
                animator.runtimeAnimatorController = triangle;
                break;
            case Transitions.Unknown:
                animator.runtimeAnimatorController = unknown;
                break;
            default: // Transitions.Ignore
                break;
        }
    }

    // Get the CORRECT clip length
    internal float GetClipLength(string clipName)
    {
	    AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
		foreach (AnimationClip clip in clips) { if(clip.name == clipName) return clip.length; }
        return 0f;
    }

    // Transition callers
    internal void TransitionIn<T>(Transitions transition = Transitions.Ignore, Action<T> doAfter = null, T parameters = default)
    {
        ChangeTransition(transition);
        StartCoroutine(CoroIn(transition, doAfter, parameters));
    }
    internal void TransitionOut<T>(Transitions transition = Transitions.Ignore, Action<T> doAfter = null, T parameters = default)
    {
        ChangeTransition(transition);
        StartCoroutine(CoroOut(transition, doAfter, parameters));
    }

    // Transition (coroutines)
    private IEnumerator CoroIn<T>(Transitions transition, Action<T> doAfter, T parameters)
    {
        // Transition in
        eventReference.enabled = false;
        animator.Play("IN");

        inTransition = true;
        yield return new WaitForSeconds(GetClipLength($"{transition} IN"));
        doAfter?.Invoke(parameters);
    }
    private IEnumerator CoroOut<T>(Transitions transition, Action<T> doAfter, T parameters)
    {
        // Transition out
        eventReference.enabled = true;
        animator.Play("OUT");

        yield return new WaitForSeconds(GetClipLength($"{transition} OUT"));
        inTransition = false;
        doAfter?.Invoke(parameters);
    }
}
