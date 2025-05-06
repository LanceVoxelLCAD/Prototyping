using UnityEngine;

[System.Serializable]
public struct SubtitleLine
{
    public string id;
    public bool overrideDurationEnabled;
    public float customDuration; // 0 to ignore, or a negative number
}
