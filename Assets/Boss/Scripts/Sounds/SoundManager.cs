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