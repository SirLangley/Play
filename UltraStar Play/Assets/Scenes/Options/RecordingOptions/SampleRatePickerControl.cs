﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SampleRatePickerControl : LabeledItemPickerControl<int>
{
    public SampleRatePickerControl(ItemPicker itemPicker)
        : base(itemPicker, new List<int>{0, 48000, 44100, 22050, 16000 })
    {
        GetLabelTextFunction = item => item <= 0
            ? TranslationManager.GetTranslation(R.Messages.options_sampleRate_auto)
            : item.ToString();
    }
}
