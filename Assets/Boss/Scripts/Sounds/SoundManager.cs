using UnityEngine;
 
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
 
    [SerializeField]
    private SoundLibrary sfxLibrary;
    [SerializeField]
    private AudioSource sfx2DSource;
 
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
 
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }
 
    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(sfxLibrary.GetClipFromName(soundName), pos);
    }
 
    // Safe to call from any scene: does nothing when the SoundManager (lives in MainMenu) isn't loaded,
    // e.g. when pressing Play straight from the Gameplay scene.
    public static void Play(string soundName, float volume = 1f)
    {
        if (Instance != null) Instance.PlaySound2D(soundName, volume);
    }

    public void PlaySound2D(string soundName, float volume = 1f)
    {
        AudioClip clip = sfxLibrary.GetClipFromName(soundName);
        if (clip != null) sfx2DSource.PlayOneShot(clip, volume);
    }

    // Looping sound that lives on its owner: it stops by itself when the owner is destroyed.
    // Goes through the same mixer group as the other SFX, so the SFX slider still controls it.
    public AudioSource CreateLoop(string soundName, GameObject owner, float volume = 1f)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.clip = sfxLibrary.GetClipFromName(soundName);
        source.outputAudioMixerGroup = sfx2DSource.outputAudioMixerGroup;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }}