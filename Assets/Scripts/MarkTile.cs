using UnityEngine;

public class MarkTile : MonoBehaviour
{
    public void OnTriggerEnter(Collider tile)
    {
        if (!LevelManager.Instance) return;
    }
}
