using TMPro;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    //this is just.. a label. for now
    public enum PickupType { RedCanister, YellowCanister, BlueCanister, GreenCanister, FakeGun }
    public PickupType pickupType;
    //public float amount = 15f;

    public GameObject pickupUIPrompt;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (pickupUIPrompt.activeSelf)
        {
            if (mainCamera != null)
            {
                pickupUIPrompt.transform.LookAt(mainCamera.transform);
            }
        }
    }

    public void ShowButtonPrompt(bool setActive)
    {
        pickupUIPrompt.SetActive(setActive);
    }

}
