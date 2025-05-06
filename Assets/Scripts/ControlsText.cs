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
}
