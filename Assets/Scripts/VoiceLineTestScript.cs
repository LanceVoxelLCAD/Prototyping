using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class VoiceLineTester : MonoBehaviour
{
    [Header("FMOD Voice Events")]
    public List<EventReference> voiceLineEvents;

    [Header("UI Setup")]
    public GameObject buttonPrefab; // A prefab with a Button + Text
    public Transform buttonContainer; // A vertical layout group parent

    private void Start()
    {
        PopulateMenu();
    }

    void PopulateMenu()
    {
        for (int i = 0; i < voiceLineEvents.Count; i++)
        {
            int index = i; // Local copy for lambda
            EventReference evt = voiceLineEvents[index];

            GameObject newButton = Instantiate(buttonPrefab, buttonContainer);
            newButton.GetComponentInChildren<Text>().text = $"Play Line {index + 1}";

            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                PlayVoiceLine(evt);
            });
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
