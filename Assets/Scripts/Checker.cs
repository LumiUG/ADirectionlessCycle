using UnityEngine;

public class Checker : MonoBehaviour
{
    public GameObject checkerTop;
    public GameObject checkerBottom;
    [Range(-1 ,1)] public int dirX = 1;
    [Range(-1, 1)] public int dirY = -1;

    private GameObject enabledChecker = null;
    [Range(-4f, 4f)] public  float speed = 1.0f;

    void Start()
    {
        enabledChecker = checkerTop;
    }

    // Checkerboard
    void Update()
    {
        // Swap checker (i mean nevermind, if it works it works for now LMAO)
        if (enabledChecker.transform.position.x >= -4.5f || enabledChecker.transform.position.x <= -8.5f || enabledChecker.transform.position.y >= 2.5f || enabledChecker.transform.position.y <= -2.5f)
        {
            enabledChecker.transform.position = transform.position;
            // enabledChecker.SetActive(false);

            // if (enabledChecker == checkerTop) enabledChecker = checkerBottom;
            // else enabledChecker = checkerTop;
            // enabledChecker.SetActive(true);
        }

        // Moves
        enabledChecker.transform.position += speed * Time.deltaTime * new Vector3(dirX, dirY, 0);
    }
}
