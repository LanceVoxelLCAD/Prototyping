using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FMODBackgroundMusic : MonoBehaviour
{
    public EventReference musicEvent;
    private EventInstance musicInstance;

    void Start()
    {
        Debug.Log("🎵 BackgroundMusic script started.");

        if (musicEvent.IsNull)
        {
            Debug.LogWarning("🚫 FMOD music event is null — assign it in the inspector.");
            return;
        }

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        if (!musicInstance.isValid())
        {
            Debug.LogError("❌ Failed to create a valid FMOD event instance.");
            return;
        }

        FMOD.RESULT result = musicInstance.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("❌ FMOD failed to start music: " + result);
        }
        else
        {
            Debug.Log("✅ FMOD music started successfully.");
        }

        musicInstance.release();
    }
}
