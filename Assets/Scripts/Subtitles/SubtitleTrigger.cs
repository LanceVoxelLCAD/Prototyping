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

    [Header("Event After Sequence")]
    public UnityEngine.Events.UnityEvent onSequenceComplete;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            //StartCoroutine(PlaySubtitlesSequentially());
            //SubtitleManager.Instance?.PlaySubtitleSequence(subtitleLines);
            SubtitleManager.Instance?.PlaySubtitleSequence(subtitleLines, OnSubtitlesComplete);
            PlayVoiceLines();

            //if (destroyAfterUse) { Destroy(gameObject); }
        }
    }

    private void PlayVoiceLines()
    {
        foreach (var evt in voiceLineEvents)
        {
            if (!evt.IsNull)
            {
                RuntimeManager.PlayOneShot(evt);
            }
        }
    }

    private void OnSubtitlesComplete()
    {
        onSequenceComplete?.Invoke();
        if (destroyAfterUse) Destroy(gameObject);
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