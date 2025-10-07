using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    //UI TABS WILL BE MOVED HERE.
    //UI HEALTH BAR WILL BE MOVED HERE.
    //STAMANA BAR WILL BE MOVED HERE.
    //RETICLE PROBABLY SHOULD BE MOVED HERE.
    //HEALED EFFECT AND DAMAGED EFFECT SHOULD BE MOVED HERE.
    //MAYBE HERE IS THE CANISTER PRESS E TO PICK UP. ("Pickup.cs" and "PickupTrigger.cs")
    public GameObject pickupUIPrompt;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowButtonPrompt(bool setActive)
    {
        pickupUIPrompt.SetActive(setActive);
    }
}
