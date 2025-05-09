using UnityEngine;
using static TransitionManager.Transitions;

public class BonusLevels : MonoBehaviour
{
    public GameObject backButton;

    void Start()
    {
        UI.I.selectors.ChangeSelected(backButton, true);
    }

    // Load a level
    public void PlayLevel(string levelID)
    {
        if (TransitionManager.I.inTransition) return;
        
        TransitionManager.I.TransitionIn(Reveal, Actions.LoadLevel, levelID);
    }
}
