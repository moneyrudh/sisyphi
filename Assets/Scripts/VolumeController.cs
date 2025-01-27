using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;

    private void Start()
    {
        masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        SFXVolumeSlider.onValueChanged.AddListener(HandleSFXVolumeChanged);

        if (SceneManager.GetActiveScene().name == Loader.Scene.MainMenu.ToString())
        {
            masterVolumeSlider.value = 1f;
            musicVolumeSlider.value = 1f;
            SFXVolumeSlider.value = 1f;
        }
        else
        {
            masterVolumeSlider.value = SoundManager.Instance.GetMasterVolume();
            musicVolumeSlider.value = SoundManager.Instance.GetMusicVolume();
            SFXVolumeSlider.value = SoundManager.Instance.GetSFXVolume();
        }
    }

    private void HandleMasterVolumeChanged(float volume)
    {
        SoundManager.Instance.SetMasterVolume(volume);
    }

    private void HandleMusicVolumeChanged(float volume)
    {
        SoundManager.Instance.SetMusicVolume(volume);
    }
    
    private void HandleSFXVolumeChanged(float volume)
    {
        SoundManager.Instance.SetSFXVolume(volume);
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (SFXVolumeSlider != null) SFXVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}
