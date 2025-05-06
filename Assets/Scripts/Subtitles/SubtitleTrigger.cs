using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    public List<string> subtitleIDs = new();
    public bool destroyAfterUse = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //StartCoroutine(PlaySubtitlesSequentially());
            SubtitleManager.Instance?.PlaySubtitleSequence(subtitleIDs);
            if (destroyAfterUse) { Destroy(gameObject); }
        }
    }

    //private IEnumerator PlaySubtitlesSequentially()
    //{
    //    foreach (string id in subtitleIDs)
    //    {
    //        SubtitleData data = SubtitleManager.Instance.GetSubtitleData(id);
    //        SubtitleManager.Instance.PlaySubtitle(id);
    //        yield return new WaitForSeconds(data.displayDuration + 0.1f); // slight buffer
    //    }
    //}
}
