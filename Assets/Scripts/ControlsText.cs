using UnityEngine;
using TMPro;

public class ControlsText : MonoBehaviour
{
    public TMP_Text controlsText;

    public void onWASD()
    {
        controlsText.text = "Move with WASD";
    }

    public void onJump()
    {
        controlsText.text = "Jump with Space";
    }

    public void onRun()
    {
        controlsText.text = "Run with Shift";
    }

    public void onGunBeam()
    {
        controlsText.text = "Fire Beam with Left Mouse";
    }

    public void onGunFoam()
    {
        controlsText.text = "Switch Firing Modes With C<br>or quick switch with Right Mouse, then Left Mouse";
    }
}
