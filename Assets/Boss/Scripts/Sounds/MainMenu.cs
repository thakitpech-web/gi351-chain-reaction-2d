using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using System;

// Put this on the Canvas in the Main scene.
// Hook the buttons' OnClick to PlayGame() and QuitGame().
public class MainMenu : MonoBehaviour
{
    [Header("Sound")]
    public AudioMixer audioMixer;
    [Header("Panel")]
    public GameObject settingPanel;
    [Header("Sound Slider")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [SerializeField] private string gameplayScene = "Gameplay";

    private void Start()
    {
        settingPanel.SetActive(false);
        Time.timeScale = 1f; // in case we came back from a paused Game Over
        // MusicManager.Instance.PlayMusic("Main");
        

        if (PlayerPrefs.HasKey("musicVolume"))
        {
            LoadVolume();
        }
        else
        {
            UpdateMusicVolume();
            UpdateSoundVolume();
        }
    }

    public void PlayGame()
    {
        SceneFader.FadeTo(gameplayScene);
        // MusicManager.Instance.PlayMusic("Level");
        // SoundManager.Instance.PlaySound2D("Button");
    }

    public void OpenSetting()
    {
        settingPanel.SetActive(true);
        // SoundManager.Instance.PlaySound2D("Button");
    }
    public void Close()
    {
        settingPanel.SetActive(false);
        // SoundManager.Instance.PlaySound2D("Button");
    }

    public void BackToMainMenu()
    {
        // SoundManager.Instance.PlaySound2D("Button");
        SceneManager.LoadScene("Main");
    }

    public void QuitGame()
    {
        SoundManager.Instance.PlaySound2D("Button");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void UpdateMusicVolume()
    {
        float volume = musicSlider.value;
        audioMixer.SetFloat("music",MathF.Log10(Mathf.Max(volume, 0.0001f))*20);
        PlayerPrefs.SetFloat("musicVolume",volume);
    }

    public void UpdateSoundVolume()
    {
        float volume = sfxSlider.value;
        audioMixer.SetFloat("SFX",MathF.Log10(Mathf.Max(volume, 0.0001f))*20);
        PlayerPrefs.SetFloat("SFXVolume",volume);
    }


    public void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");
        UpdateMusicVolume();
        UpdateSoundVolume();
    }
}
    
    