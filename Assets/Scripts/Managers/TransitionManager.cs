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
    internal enum Transitions { Crossfade, Reveal, Swipe };
    internal EventSystem eventReference;
    internal Coroutine currentTransition = null;

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
        eventReference = eventReference = EventSystem.current;
        animator = GetComponent<Animator>();
        crossfade = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Crossfade/Base");
        reveal = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Reveal/Base");
        swipe = Resources.Load<AnimatorOverrideController>("Animations/Transitions/Swipe/Base");

        // Play default
        ChangeTransition(Transitions.Reveal);
    }

    // Transitions out of a black screen
    private void SceneLoad(Scene scene, LoadSceneMode sceneMode) { TransitionOut<string>(Transitions.Reveal); }

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

    // Transition callers
    internal void TransitionIn<T>(Transitions transition, Action<T> doAfter = null, T parameters = default)
    {
        StartCoroutine(CoroIn(transition, doAfter, parameters));
    }
    internal void TransitionOut<T>(Transitions transition, Action<T> doAfter = null, T parameters = default)
    {
        StartCoroutine(CoroOut(transition, doAfter, parameters));
    }

    // Transition (coroutines)
    private IEnumerator CoroIn<T>(Transitions transition, Action<T> doAfter, T parameters)
    {
        // Transition in
        eventReference.enabled = false;
        ChangeTransition(transition);
        animator.Play("IN");

        inTransition = true;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        doAfter?.Invoke(parameters);
    }
    private IEnumerator CoroOut<T>(Transitions transition, Action<T> doAfter, T parameters)
    {
        // Transition out
        eventReference.enabled = true;
        ChangeTransition(transition);
        animator.Play("OUT");

        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        inTransition = false;
        doAfter?.Invoke(parameters);
    }
}
