using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class FMODHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("FMOD Event")]
    public EventReference hoverSound;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hoverSound.IsNull)
        {
            RuntimeManager.PlayOneShot(hoverSound);
        }
    }
}
