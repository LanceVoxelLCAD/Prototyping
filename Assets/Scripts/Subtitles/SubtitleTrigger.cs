using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    public string subtitleID;
    public bool destroyAfterUse = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SubtitleManager.Instance?.PlaySubtitle(subtitleID);
            if (destroyAfterUse) { Destroy(gameObject); }
        }
    }
}
