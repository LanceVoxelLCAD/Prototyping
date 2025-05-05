using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Buttons")]
    public Button backButton;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;

    private VCA musicVCA;
    private VCA sfxVCA;
    private VCA voiceVCA;

    void Start()
    {
        // VCA setup
        musicVCA = RuntimeManager.GetVCA("vca:/Music");
        sfxVCA = RuntimeManager.GetVCA("vca:/SFX");
        voiceVCA = RuntimeManager.GetVCA("vca:/Voice");

        float musicVol = PlayerPrefs.GetFloat("Volume_Music", 1f);
        float sfxVol = PlayerPrefs.GetFloat("Volume_SFX", 1f);
        float voiceVol = PlayerPrefs.GetFloat("Volume_Voice", 1f);

        musicSlider.value = musicVol;
        sfxSlider.value = sfxVol;
        voiceSlider.value = voiceVol;

        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
        SetVoiceVolume(voiceVol);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        voiceSlider.onValueChanged.AddListener(SetVoiceVolume);

        backButton.onClick.AddListener(BackToPauseMenu);
    }

    public void SetMusicVolume(float volume)
    {
        musicVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_Music", volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_SFX", volume);
    }

    public void SetVoiceVolume(float volume)
    {
        voiceVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_Voice", volume);
    }

    void BackToPauseMenu()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
}
