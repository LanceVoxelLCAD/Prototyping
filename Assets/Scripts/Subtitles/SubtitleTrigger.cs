using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class SubtitleTrigger : MonoBehaviour
{
    //public List<string> subtitleIDs = new();
    public bool destroyAfterUse = true;
    public List<SubtitleLine> subtitleLines;

    [Header("FMOD Voice Events")]
    public List<EventReference> voiceLineEvents;
    private EventReference evt;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //StartCoroutine(PlaySubtitlesSequentially());
            SubtitleManager.Instance?.PlaySubtitleSequence(subtitleLines);

            foreach (var evt in voiceLineEvents)
            {
                PlayVoiceLine(evt);

                if (destroyAfterUse) { Destroy(gameObject); }
            }
        }
        void PlayVoiceLine(EventReference evt)
        {
            if (!evt.IsNull)
            {
                RuntimeManager.PlayOneShot(evt);
            }
        }
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