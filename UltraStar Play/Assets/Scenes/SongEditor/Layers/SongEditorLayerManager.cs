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

public class SongEditorLayerManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    private readonly Dictionary<ESongEditorLayer, SongEditorLayer> layerKeyToLayerMap = CreateLayerKeyToLayerMap();

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;
    
    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is MovedNotesToVoiceEvent mntve)
        {
            mntve.notes
                .Where(note => note.Sentence != null)
                .ForEach(note => RemoveNoteFromAllLayers(note));
        }
    }
    
    public void AddNoteToLayer(ESongEditorLayer layerKey, Note note)
    {
        layerKeyToLayerMap[layerKey].AddNote(note);
    }

    public void ClearLayer(ESongEditorLayer layerKey)
    {
        layerKeyToLayerMap[layerKey].ClearNotes();
    }

    public List<Note> GetNotes(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].GetNotes();
    }

    public Color GetColor(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].Color;
    }

    public bool IsLayerEnabled(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].IsEnabled;
    }

    public void SetLayerEnabled(ESongEditorLayer layerKey, bool newValue)
    {
        layerKeyToLayerMap[layerKey].IsEnabled = newValue;
    }

    public List<SongEditorLayer> GetLayers()
    {
        return new List<SongEditorLayer>(layerKeyToLayerMap.Values);
    }

    private static Dictionary<ESongEditorLayer, SongEditorLayer> CreateLayerKeyToLayerMap()
    {
        Dictionary<ESongEditorLayer, SongEditorLayer> result = new Dictionary<ESongEditorLayer, SongEditorLayer>();
        List<ESongEditorLayer> layerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();
        foreach (ESongEditorLayer layerKey in layerKeys)
        {
            result.Add(layerKey, new SongEditorLayer(layerKey));
        }
        result[ESongEditorLayer.MicRecording].Color = Colors.coral;
        result[ESongEditorLayer.ButtonRecording].Color = Colors.indigo;
        result[ESongEditorLayer.CopyPaste].Color = Colors.CreateColor("#F08080", 200);
        result[ESongEditorLayer.MidiFile].Color = Colors.mediumVioletRed;
        return result;
    }

    public List<Note> GetAllNotes()
    {
        List<Note> notes = new List<Note>();
        foreach (ESongEditorLayer layerKey in layerKeyToLayerMap.Keys)
        {
            List<Note> notesOfLayer = GetNotes(layerKey);
            notes.AddRange(notesOfLayer);
        }
        return notes;
    }

    public List<Note> GetAllVisibleNotes()
    {
        return GetAllNotes()
            .Where(IsVisible)
            .ToList();
    }

    public void RemoveNoteFromAllLayers(Note note)
    {
        foreach (SongEditorLayer layer in layerKeyToLayerMap.Values)
        {
            layer.RemoveNote(note);
        }
    }

    public bool IsVisible(Note note)
    {
        return note.Sentence?.Voice == null
            || !settings.SongEditorSettings.HideVoices.Contains(note.Sentence.Voice.Name);
    }
}
