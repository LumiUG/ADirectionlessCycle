using UnityEngine;
using static TransitionManager.Transitions;

public class BonusLevels : MonoBehaviour
{
    public GameObject backButton;

    void Start()
    {
        UI.Instance.selectors.ChangeSelected(backButton, true);
    }

    // Load a level
    public void PlayLevel(string levelID)
    {
        if (TransitionManager.Instance.inTransition) return;
        
        TransitionManager.Instance.TransitionIn(Reveal, LevelManager.Instance.ActionLoadLevel, levelID);
    }
}
