using UnityEngine;
using UnityEngine.UI;

public enum VoiceGender { Male = 0, Female = 1 }

public class VoiceSelectionMenu : MonoBehaviour
{
    public Button maleButton;
    public Button femaleButton;
    public Button settingsToggleButton; // NEW

    public GameObject menuUI;

    public static VoiceGender SelectedVoiceGender { get; private set; } = VoiceGender.Male;

    private void Start()
    {
        maleButton.onClick.AddListener(() => SelectGender(VoiceGender.Male));
        femaleButton.onClick.AddListener(() => SelectGender(VoiceGender.Female));
        settingsToggleButton.onClick.AddListener(ToggleMenu); // ← NEW

        menuUI.SetActive(false); // Hide on start
    }

    void SelectGender(VoiceGender gender)
    {
        SelectedVoiceGender = gender;
        Debug.Log("Selected Voice: " + gender);
    }

    void ToggleMenu()
    {
        bool isActive = menuUI.activeSelf;

        menuUI.SetActive(!isActive);

        Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isActive;

        Time.timeScale = isActive ? 1f : 0f; // Optional pause
    }
}
