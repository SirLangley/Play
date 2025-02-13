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

public class ApplyBpmAndAdjustNoteLengthButton : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public InputField newBpmInputField;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private ApplyBpmAndAdjustNoteLengthAction applyBpmAndAdjustNoteLengthAction;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(newBpmInputField.text, out float newBpm))
            {
                applyBpmAndAdjustNoteLengthAction.ExecuteAndNotify(newBpm);
            }
        });
    }
}
