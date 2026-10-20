using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Put this on a settings panel that has a music slider and an SFX slider (any scene).
// The sliders are hooked up in code, so their OnValueChanged lists can stay empty.
// Uses the same PlayerPrefs keys as MainMenu, so the volume carries over between scenes.
public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void OnEnable()
    {
        // Show the saved volume every time the panel opens
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("musicVolume", musicSlider.value));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", sfxSlider.value));
    }

    private void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("music", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    private void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
}
