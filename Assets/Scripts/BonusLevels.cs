using UnityEngine;
using static TransitionManager.Transitions;

public class BonusLevels : MonoBehaviour
{
    public GameObject backButton;
    public GameObject[] outlines; 

    void Start()
    {
        UI.I.selectors.ChangeSelected(backButton, true);
        outlines[0].SetActive(GameManager.save.game.levels.Find(level => level.levelID == "CODE/Despair") != null);
        outlines[1].SetActive(GameManager.save.game.levels.Find(level => level.levelID == "CODE/Quiz") != null);
    }

    // Load a level
    public void PlayLevel(string levelID)
    {
        if (TransitionManager.I.inTransition) return;
        
        TransitionManager.I.TransitionIn(Reveal, Actions.LoadLevel, levelID);
    }
}
