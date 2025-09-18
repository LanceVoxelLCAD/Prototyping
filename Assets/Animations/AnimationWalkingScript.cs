using UnityEngine;
using FMODUnity;

public class FootstepAudio : MonoBehaviour
{
    public EventReference footstepSound;
    public CameraController camController;

    public void PlayFootstep()
    {
        //Debug.Log("playing footstep from body"); //finally oh my god
        camController.TriggerFootstep();
        RuntimeManager.PlayOneShot(footstepSound, transform.position);
    }
}
