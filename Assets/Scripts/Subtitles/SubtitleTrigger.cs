using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    public string subtitleID;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SubtitleManager.Instance?.PlaySubtitle(subtitleID);
        }
    }
}
