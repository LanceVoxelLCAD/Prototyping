using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;


public enum VoiceGender { Male = 0, Female = 1 }

public class VoiceSelectionMenu : MonoBehaviour
{
    public Button maleButton;
    public Button femaleButton;
    //public Button settingsToggleButton;
    public Button closeButton;

    public GameObject menuUI;
    public GameObject pauseMenuUI;

    public Slider pitchSlider;
    public EventReference pitchSampleEvent;

    public static float SelectedVoicePitch { get; private set; } = 0f;

    private EventInstance previewInstance;


    public static VoiceGender SelectedVoiceGender { get; private set; } = VoiceGender.Male;

    private void Start()
    {
        maleButton.onClick.AddListener(() => SelectGender(VoiceGender.Male));
        femaleButton.onClick.AddListener(() => SelectGender(VoiceGender.Female));
        //settingsToggleButton.onClick.AddListener(ToggleMenu);
        closeButton.onClick.AddListener(CloseMenu);
        pitchSlider.onValueChanged.AddListener(OnPitchSliderChanged);
        pitchSlider.value = 0f;


        menuUI.SetActive(false); // Hide on start
    }

    void CloseMenu()
    {
        menuUI.SetActive(false);       // Hide voice selection
        pauseMenuUI.SetActive(true);   // Show pause menu

        // Keep cursor visible and unlocked since we're staying in UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f; // Still paused
    }


    void OnPitchSliderChanged(float value)
    {
        SelectedVoicePitch = value;

        // Play or update sample
        if (pitchSampleEvent.IsNull)
            return;

        // Stop old preview if it's playing
        previewInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        previewInstance.release();

        // Start new instance with updated pitch
        previewInstance = RuntimeManager.CreateInstance(pitchSampleEvent);
        previewInstance.setParameterByName("VoiceGender", (int)SelectedVoiceGender);
        previewInstance.setParameterByName("VoicePitch", SelectedVoicePitch);
        previewInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Camera.main.transform)); // non-spatial 2D preview
        previewInstance.start();
    }

    void SelectGender(VoiceGender gender)
    {
        SelectedVoiceGender = gender;
        Debug.Log("Selected Voice: " + gender);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuUI.activeSelf)
            {
                CloseMenu();
            }
        }
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
