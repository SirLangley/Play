﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AutoCalibrateMicDelayButton : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public MicPitchTracker micPitchTracker;

    [InjectedInInspector]
    public MicDelayNumberSpinner micDelaySpinner;

    [Inject]
    private UiManager uiManager;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioSource;

    // The audio clips and midi notes that are played for calibration.
    public List<AudioClip> audioClips;
    public List<string> midiNoteNames;

    private bool isCalibrationInProgress;

    private float startTimeInSeconds;
    private readonly float timeoutInSeconds = 2;
    private float pauseTime = float.MinValue;

    private List<int> delaysInMillis = new List<int>();
    private int currentIteration;

    public MicProfile MicProfile { get; set; }

    void Awake()
    {
        // Sanity check
        if (midiNoteNames.Count == 0)
        {
            throw new UnityException("midiNoteNames not set");
        }
        if (audioClips.Count == 0)
        {
            throw new UnityException("audioClips not set");
        }
        if (audioClips.Count != midiNoteNames.Count)
        {
            throw new UnityException("audioClips and midiNotes must have same length");
        }
    }

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => OnStartCalibration());
        micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
    }

    void Update()
    {
        if (!isCalibrationInProgress)
        {
            return;
        }

        if (pauseTime > 0)
        {
            pauseTime -= Time.deltaTime;
        }
        else if (pauseTime > float.MinValue)
        {
            pauseTime = float.MinValue;
            StartIteration();
        }

        if ((startTimeInSeconds + timeoutInSeconds) < Time.time)
        {
            OnCalibrationTimedOut();
        }
    }

    private void OnStartCalibration()
    {
        if (isCalibrationInProgress)
        {
            return;
        }
        isCalibrationInProgress = true;

        delaysInMillis = new List<int>();
        currentIteration = 0;
        StartIteration();
    }

    private void OnCalibrationTimedOut()
    {
        Debug.Log("Mic delay calibration - timeout");
        uiManager.CreateNotification("Calibration timed out", Colors.red);
        audioSource.Stop();
        isCalibrationInProgress = false;
    }

    private void OnEndCalibration()
    {
        Debug.Log($"Mic delay calibration - avg delay of {delaysInMillis.Count} values: {delaysInMillis.Average()}");
        audioSource.Stop();
        isCalibrationInProgress = false;

        if (MicProfile != null)
        {
            MicProfile.DelayInMillis = (int)delaysInMillis.Average();
            micDelaySpinner.SetMicProfile(MicProfile);
        }
    }

    private void StartIteration()
    {
        startTimeInSeconds = Time.time;
        audioSource.clip = audioClips[currentIteration];
        audioSource.Play();
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (pitchEvent == null || !isCalibrationInProgress || pauseTime > 0)
        {
            return;
        }

        string targetMidiNoteName = midiNoteNames[currentIteration];
        if (MidiUtils.GetAbsoluteName(pitchEvent.MidiNote) == targetMidiNoteName)
        {
            audioSource.Stop();
            float delayInSeconds = Time.time - startTimeInSeconds;
            int delayInMillis = (int)(delayInSeconds * 1000);
            delaysInMillis.Add(delayInMillis);
            Debug.Log($"Mic delay calibration - delay of iteration {currentIteration}: {delayInMillis}");

            currentIteration++;
            if (currentIteration >= midiNoteNames.Count)
            {
                OnEndCalibration();
            }
            else
            {
                // Wait a bit for silence before the next iteration.
                pauseTime = 0.5f;
            }
        }
        else
        {
            Debug.Log("Mic delay calibration - wrong pitch: " + MidiUtils.GetAbsoluteName(pitchEvent.MidiNote));
        }
    }
}
