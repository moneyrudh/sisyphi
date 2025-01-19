using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.PlayerLoop;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;

    public bool loop = false;
    public bool is3D = false;
    public float maxDistance = 20f;
    public float spatialBlend = 1f;
    public AudioMixerGroup mixerGroup;

    [HideInInspector]
    public AudioSource source;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public AudioMixer audioMixer;
    public Sound[] sounds;

    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";
    private const string MASTER_VOLUME = "MasterVolume";

    private const float MIN_VOLUME = -80f;
    private const float MAX_VOLUME = 0f;

    private Dictionary<string, Sound> soundDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Play("Theme");
        SetMasterVolume(0f);
    }

    private void InitializeSounds()
    {
        soundDictionary = new Dictionary<string, Sound>();

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;

            sound.source.spatialBlend = sound.is3D ? sound.spatialBlend : 0f;
            if (sound.is3D)
            {
                sound.source.rolloffMode = AudioRolloffMode.Linear;
                sound.source.maxDistance = sound.maxDistance;
                sound.source.spatialize = true;
                sound.source.spatializePostEffects = true;
            }

            if (sound.mixerGroup != null)
            {
                sound.source.outputAudioMixerGroup = sound.mixerGroup;
            }

            soundDictionary.Add(sound.name, sound);
        }
    }

    // Attach a sound to a GameObject (for continuous moving sounds)
    public AudioSource AttachSound(string soundName, GameObject targetObject, float fadeInDuration = 0.5f)
    {
        Debug.Log($"Attempting to play {soundName}");
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
        Debug.Log($"PLAYING {soundName}");
            // Add AudioSource to the target object
            AudioSource attachedSource = targetObject.AddComponent<AudioSource>();
            
            // Copy settings from original sound
            attachedSource.clip = sound.clip;
            attachedSource.volume = sound.volume;
            attachedSource.pitch = sound.pitch;
            attachedSource.loop = sound.loop;
            attachedSource.spatialBlend = sound.spatialBlend;
            attachedSource.maxDistance = sound.maxDistance;
            attachedSource.rolloffMode = AudioRolloffMode.Linear;
            attachedSource.outputAudioMixerGroup = sound.mixerGroup;
            
            attachedSource.Play();
            StartCoroutine(FadeAttachedSourceCoroutine(attachedSource, 0f, sound.volume, fadeInDuration));
            return attachedSource;
        }
        
        Debug.LogWarning($"Sound {soundName} not found!");
        return null;
    }

    // Add this new method
    public void DetachSound(AudioSource attachedSource, float fadeOutDuration = 0.5f)
    {
        if (attachedSource != null)
        {
            StartCoroutine(FadeAndDestroyCoroutine(attachedSource, fadeOutDuration));
        }
    }

    private IEnumerator FadeAttachedSourceCoroutine(AudioSource source, float startVolume, float endVolume, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            if (source != null)
            {
                source.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            }
            yield return null;
        }
        
        if (source != null)
        {
            source.volume = endVolume;
        }
    }

    // Add this new coroutine
    private IEnumerator FadeAndDestroyCoroutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            if (source != null)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            }
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            Destroy(source);
        }
    }

    public void PlayAtPosition(string soundName, Vector3 position)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (sound.is3D)
            {
                GameObject tempAudio = new GameObject("TempAudio");
                tempAudio.transform.position = position;
                AudioSource tempSource = tempAudio.AddComponent<AudioSource>();

                tempSource.clip = sound.clip;
                tempSource.volume = sound.volume;
                tempSource.pitch = sound.pitch;
                tempSource.spatialBlend = sound.spatialBlend;
                tempSource.maxDistance = sound.maxDistance;
                tempSource.rolloffMode = AudioRolloffMode.Linear;
                tempSource.outputAudioMixerGroup = sound.mixerGroup;

                tempSource.PlayOneShot(tempSource.clip, tempSource.volume);
                Destroy(tempAudio, sound.clip.length + 0.1f);
            }
            else
            {
                sound.source.PlayOneShot(sound.clip, sound.volume);
            }
        }
        else
        {
            Debug.LogWarning($"Sound {soundName} not found!");
        }
    }

    public void Play(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (sound.is3D)
            {
                Debug.LogWarning($"Trying to play 3D sound {soundName} without position! Use PlayAtPosition instead.");
                return;
            }
            sound.source.Play();
        }
        else
        {
            Debug.LogWarning($"Sound {soundName} not found!");
        }
    }

    public void Stop(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            sound.source.Stop();
        }
    }

    public bool IsPlaying(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (sound.source.isPlaying) return true;
        }
        return false;
    }

    public void StopAll()
    {
        foreach (var sound in soundDictionary.Values)
        {
            sound.source.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        SetVolume(MUSIC_VOLUME, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetVolume(SFX_VOLUME, volume);
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(MASTER_VOLUME, volume);
    }

    public float GetMusicVolume()
    {
        if (audioMixer.GetFloat(MUSIC_VOLUME, out float value))
        {
            return Mathf.InverseLerp(MIN_VOLUME, MAX_VOLUME, value);
        }
        return 0f;
    }

    public float GetSFXVolume()
    {
        if (audioMixer.GetFloat(SFX_VOLUME, out float value))
        {
            return Mathf.InverseLerp(MIN_VOLUME, MAX_VOLUME, value);
        }
        return 0f;
    }

    public float GetMasterVolume()
    {
        if (audioMixer.GetFloat(MASTER_VOLUME, out float value))
        {
            return Mathf.InverseLerp(MIN_VOLUME, MAX_VOLUME, value);
        }
        return 0f;
    }

    private void SetVolume(string parameter, float normalizedVolume)
    {
        float mixerVolume = Mathf.Lerp(MIN_VOLUME, MAX_VOLUME, normalizedVolume);
        audioMixer.SetFloat(parameter, mixerVolume);
    }

    public void PlayOneShot(string soundName, float volumeScale = 1f)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            sound.source.PlayOneShot(sound.clip, volumeScale * sound.volume);
        }
    }

    public void FadeIn(string soundName, float duration)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            StartCoroutine(FadeCoroutine(sound.source, 0f, sound.volume, duration, true));
        }
    }

    public void FadeOut(string soundName, float duration)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            StartCoroutine(FadeCoroutine(sound.source, sound.source.volume, 0f, duration, false));
        }
    }

    private IEnumerator FadeCoroutine(AudioSource source, float startVolume, float endVolume, float duration, bool playOnStart)
    {
        if (playOnStart)
        {
            source.volume = startVolume;
            source.Play();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            yield return null;
        }
        
        source.volume = endVolume;
        if (!playOnStart && endVolume <= 0f)
        {
            source.Stop();  
        }
    }
}
