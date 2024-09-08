using UnityEngine;

public class Checker : MonoBehaviour
{
    public GameObject checker;
    [Range(-1 , 1)] public int dirX = 1;
    [Range(-1, 1)] public int dirY = -1;

    [Range(-4f, 4f)] public float speed = 1.0f;

    // Checkerboard
    void Update()
    {
        // Move back checkers
        if (checker.transform.position.x >= -4.5f || checker.transform.position.x <= -8.5f) checker.transform.position = new(transform.position.x, checker.transform.position.y, checker.transform.position.z);
        if (checker.transform.position.y >= 1.5f || checker.transform.position.y <= -2.5f) checker.transform.position = new (checker.transform.position.x, transform.position.y, checker.transform.position.z);

        // Moves
        checker.transform.position += speed * Time.deltaTime * new Vector3(dirX, dirY, 0);
    }
}
