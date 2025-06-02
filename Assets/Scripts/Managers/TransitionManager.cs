using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [HideInInspector] public static TransitionManager I;
    [HideInInspector] public Animator animator;

    internal bool inTransition = true;
    internal enum Transitions { Ignore = 0, Crossfade, Reveal, Swipe, Triangle, Unknown, Refresh, Dive, Load, Finale };
    internal EventSystem eventReference;
    internal Coroutine currentTransition = null;
    private bool firstTransition = true;

    [Header("Transitions")]
    [SerializeField] private AnimatorOverrideController crossfade;
    [SerializeField] private AnimatorOverrideController reveal;
    [SerializeField] private AnimatorOverrideController swipe;
    [SerializeField] private AnimatorOverrideController triangle;
    [SerializeField] private AnimatorOverrideController unknown;
    [SerializeField] private AnimatorOverrideController refresh;
    [SerializeField] private AnimatorOverrideController dive;
    [SerializeField] private AnimatorOverrideController load;
    [SerializeField] private AnimatorOverrideController finale;

    void Awake()
    {
        // Singleton (TransManager has persistence)
        if (!I) { I = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Add to sceneloaded event
        SceneManager.sceneLoaded += SceneLoad;

        // Get overrides & animator
        eventReference = eventReference = EventSystem.current;
        animator = GetComponent<Animator>();

        // Play default
        ChangeTransition(Transitions.Load);
    }

    private void Start() => eventReference.enabled = false;

    // Transitions out of a black screen
    private void SceneLoad(Scene scene, LoadSceneMode sceneMode) { TransitionOut<string>(); }

    // Change the current animator controller
    internal void ChangeTransition(Transitions transition)
    {
        RuntimeAnimatorController controller = transition switch
        {
            Transitions.Crossfade => crossfade,
            Transitions.Reveal => reveal,
            Transitions.Swipe => swipe,
            Transitions.Triangle => triangle,
            Transitions.Unknown => unknown,
            Transitions.Refresh => refresh,
            Transitions.Dive => dive,
            Transitions.Load => load,
            Transitions.Finale => finale,
            _ => null
        };

        if (controller != null) animator.runtimeAnimatorController = controller;
    }

    // Get the CORRECT clip length
    internal float GetClipLength(string clipName)
    {
	    AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
		foreach (AnimationClip clip in clips) { if (clip.name == clipName) return clip.length; }
        return 0f;
    }

    // Transition callers
    internal void TransitionIn<T>(Transitions transition = Transitions.Ignore, Action<T> doAfter = null, T parameters = default)
    {
        if (inTransition) { doAfter?.Invoke(parameters); return; }
        if (transition == Transitions.Load) transition = Transitions.Reveal;

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
        if (firstTransition) { firstTransition = false; yield return new WaitForSeconds(3f); eventReference.enabled = true; }
        inTransition = false;
        doAfter?.Invoke(parameters);
    }
}
