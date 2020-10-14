﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeableAudio : Themeable
{
    [Delayed]
    public string audioPath;
    public AudioSource target;

#if UNITY_EDITOR
    private string lastAudioPath;

    void Update()
    {
        if (lastAudioPath != audioPath)
        {
            lastAudioPath = audioPath;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
    {
        if (theme == null)
        {
            Debug.LogError("Theme is null", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(audioPath))
        {
            Debug.LogWarning($"Missing audio file name", gameObject);
            return;
        }

        AudioSource targetAudioSource = target != null ? target : GetComponent<AudioSource>();
        if (targetAudioSource == null)
        {
            Debug.LogWarning($"Target is null and GameObject does not have an AudioSource Component", gameObject);
            return;
        }

        AudioManager.Instance.LoadAudioClipFromUri(GetStreamingAssetsUri(theme, audioPath),
                (loadedAudioClip) => target.clip = loadedAudioClip);
    }
}
