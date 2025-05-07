using UnityEngine;
using FMODUnity;

public class FootstepAudio : MonoBehaviour
{
    public EventReference footstepSound;

    public void PlayFootstep()
    {
        RuntimeManager.PlayOneShot(footstepSound, transform.position);
    }
}
