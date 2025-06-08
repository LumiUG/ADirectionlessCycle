using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Trial", menuName = "Trial Scriptable")]
[Serializable]
public class TrialScriptable : ScriptableObject
{
    public string levelID;
    public int vanillaMoves = -1;
    public int cycleMoves = -1;
}