using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BackgroundMusic : MonoBehaviour
{
    public EventReference musicEvent;

    private EventInstance musicInstance;

    void Start()
    {
        if (musicEvent.IsNull)
        {
            Debug.LogWarning("No music event assigned to BackgroundMusic.");
            return;
        }

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
        musicInstance.release(); // Automatically cleaned up when playback finishes or loop ends
    }
}
