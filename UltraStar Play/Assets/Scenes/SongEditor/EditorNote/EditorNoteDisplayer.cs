﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UI;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{

    [InjectedInInspector]
    public EditorUiNote notePrefab;
    [InjectedInInspector]
    public RectTransform noteContainer;

    [InjectedInInspector]
    public DynamicallyCreatedImage sentenceLinesImage;

    [InjectedInInspector]
    public EditorUiSentence sentenceMarkerRectanglePrefab;

    [InjectedInInspector]
    public RectTransform sentenceMarkerRectangleContainer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    private readonly Dictionary<Voice, List<Sentence>> voiceToSortedSentencesMap = new Dictionary<Voice, List<Sentence>>();

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorUiNote> noteToEditorUiNoteMap = new Dictionary<Note, EditorUiNote>();
    private readonly Dictionary<Sentence, EditorUiSentence> sentenceToEditorUiSentenceMap = new Dictionary<Sentence, EditorUiSentence>();

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(evt =>
        {
            if (evt is LoadedMementoEvent)
            {
                // The object instances have changed. All maps must be cleared.
                noteContainer.DestroyAllDirectChildren();
                noteToEditorUiNoteMap.Clear();
                sentenceMarkerRectangleContainer.DestroyAllDirectChildren();
                sentenceToEditorUiSentenceMap.Clear();
            }
            ReloadSentences();
            UpdateNotesAndSentences();
        });
    }

    void Start()
    {
        noteContainer.DestroyAllDirectChildren();
        noteToEditorUiNoteMap.Clear();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();
        sentenceToEditorUiSentenceMap.Clear();

        ReloadSentences();

        noteArea.ViewportEventStream.Subscribe(_ =>
        {
            UpdateNotesAndSentences();
        });

        songMetaChangeEventStream
            .Subscribe(evt =>
            {
                if (evt is SentencesDeletedEvent sde)
                {
                    sde.Sentences.ForEach(sentence => DeleteSentence(sentence));
                }
            });

        foreach (ESongEditorLayer layer in EnumUtils.GetValuesAsList<ESongEditorLayer>())
        {
            songEditorLayerManager
                .ObserveEveryValueChanged(it => it.IsLayerEnabled(layer))
                .Subscribe(_ => UpdateNotes());
        }

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.HideVoices.Count)
            .Subscribe(_ => OnHideVoicesChanged())
            .AddTo(this);
    }

    private void DeleteSentence(Sentence sentence)
    {
        if (sentenceToEditorUiSentenceMap.TryGetValue(sentence, out EditorUiSentence uiSentence))
        {
            Destroy(uiSentence.gameObject);
            sentenceToEditorUiSentenceMap.Remove(sentence);
        }
    }

    private void OnHideVoicesChanged()
    {
        // Remove notes of hidden voices
        List<Note> notVisibleNotes = noteToEditorUiNoteMap.Keys
            .Where(note => !IsVoiceVisible(note.Sentence?.Voice))
            .ToList();
        notVisibleNotes.ForEach(note => DeleteNote(note));

        // Remove sentences of hidden voices
        List<Sentence> notVisibleSentences = sentenceToEditorUiSentenceMap.Keys
            .Where(sentence => !IsVoiceVisible(sentence.Voice))
            .ToList();
        notVisibleSentences.ForEach(sentence => DeleteSentence(sentence));

        // Draw any notes that are now (again) visible.
        UpdateNotesAndSentences();
    }

    public bool IsVoiceVisible(Voice voice)
    {
        if (voice == null)
        {
            return true;
        }

        bool isHidden = settings.SongEditorSettings.HideVoices.Contains(voice.Name)
            || (voice.Name == Voice.soloVoiceName
                && settings.SongEditorSettings.HideVoices.Contains(Voice.firstVoiceName));
        return !isHidden;
    }

    public void ClearUiNotes()
    {
        noteContainer.DestroyAllDirectChildren();
        noteToEditorUiNoteMap.Clear();
    }

    public void ReloadSentences()
    {
        voiceToSortedSentencesMap.Clear();
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            List<Sentence> sortedSentences = new List<Sentence>(voice.Sentences);
            sortedSentences.Sort(Sentence.comparerByStartBeat);
            voiceToSortedSentencesMap.Add(voice, sortedSentences);
        }
    }

    public void UpdateNotesAndSentences()
    {
        UpdateNotes();
        UpdateSentences();
    }

    public void UpdateSentences()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        sentenceLinesImage.ClearTexture();

        IEnumerable<Voice> voices = songMeta.GetVoices();

        foreach (Voice voice in voices)
        {
            if (IsVoiceVisible(voice))
            {
                DrawSentencesInVoice(voice);
            }
        }
        sentenceLinesImage.ApplyTexture();
    }

    private void DrawSentencesInVoice(Voice voice)
    {
        int viewportWidthInBeats = noteArea.MaxBeatInViewport - noteArea.MinBeatInViewport;
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];

        int sentenceIndex = 0;
        foreach (Sentence sentence in sortedSentencesOfVoice)
        {
            if (noteArea.IsInViewport(sentence))
            {
                float xStartPercent = (float)noteArea.GetHorizontalPositionForBeat(sentence.MinBeat);
                float xEndPercent = (float)noteArea.GetHorizontalPositionForBeat(sentence.ExtendedMaxBeat);

                // Do not draw the sentence marker lines, when there are too many beats
                if (viewportWidthInBeats < 1200)
                {
                    CreateSentenceMarkerLine(xStartPercent, Colors.saddleBrown, 0);
                    CreateSentenceMarkerLine(xEndPercent, Colors.black, 20);
                }

                UpdateOrCreateUiSentence(sentence, xStartPercent, xEndPercent, sentenceIndex);
            }
            else
            {
                if (sentenceToEditorUiSentenceMap.TryGetValue(sentence, out EditorUiSentence uiSentence))
                {
                    Destroy(uiSentence.gameObject);
                }
                sentenceToEditorUiSentenceMap.Remove(sentence);
            }
            sentenceIndex++;
        }
    }

    public void UpdateNotes()
    {
        if (gameObject == null 
            || !gameObject.activeInHierarchy)
        {
            return;
        }
        DestroyUiNotesOutsideOfViewport();

        DrawNotesInSongFile();
        DrawNotesInLayers();
    }

    public EditorUiNote GetUiNoteForNote(Note note)
    {
        if (noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote uiNote))
        {
            return uiNote;
        }
        else
        {
            return null;
        }
    }

    public void DeleteUiNote(EditorUiNote uiNote)
    {
        noteToEditorUiNoteMap.Remove(uiNote.Note);
        Destroy(uiNote.gameObject);
    }

    public void DeleteNote(Note note)
    {
        if (noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote uiNote))
        {
            DeleteUiNote(uiNote);
        }
    }

    private void DestroyUiNotesOutsideOfViewport()
    {
        ICollection<EditorUiNote> editorUiNotes = new List<EditorUiNote>(noteToEditorUiNoteMap.Values);
        foreach (EditorUiNote editorUiNote in editorUiNotes)
        {
            Note note = editorUiNote.Note;
            if (!noteArea.IsInViewport(note))
            {
                Destroy(editorUiNote.gameObject);
                noteToEditorUiNoteMap.Remove(note);
            }
        }
    }

    private void DrawNotesInLayers()
    {
        foreach (ESongEditorLayer layerKey in songEditorLayerKeys)
        {
            if (songEditorLayerManager.IsLayerEnabled(layerKey))
            {
                DrawNotesInLayer(layerKey);
            }
        }
    }

    private void DrawNotesInLayer(ESongEditorLayer layerKey)
    {
        List<Note> notesInLayer = songEditorLayerManager.GetNotes(layerKey)
            .Where(note => note.Sentence == null).ToList();
        List<Note> notesInViewport = notesInLayer
            .Where(note => noteArea.IsInViewport(note))
            .ToList();

        Color layerColor = songEditorLayerManager.GetColor(layerKey);
        foreach (Note note in notesInViewport)
        {
            EditorUiNote uiNote = UpdateOrCreateUiNote(note);
            if (uiNote != null)
            {
                uiNote.SetColor(layerColor);
            }
        }
    }

    private void DrawNotesInSongFile()
    {
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            if (IsVoiceVisible(voice))
            {
                DrawNotesInVoice(voice);
            }
        }
    }

    private void DrawNotesInVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];
        List<Sentence> sentencesInViewport = sortedSentencesOfVoice
            .Where(sentence => noteArea.IsInViewport(sentence))
            .ToList();

        List<Note> notesInViewport = sentencesInViewport
                .SelectMany(sentence => sentence.Notes)
                .Where(note => noteArea.IsInViewport(note))
                .ToList();

        foreach (Note note in notesInViewport)
        {
            UpdateOrCreateUiNote(note);
        }
    }

    private void UpdateOrCreateUiSentence(Sentence sentence, float xStartPercent, float xEndPercent, int sentenceIndex)
    {
        if (!sentenceToEditorUiSentenceMap.TryGetValue(sentence, out EditorUiSentence uiSentence))
        {
            uiSentence = Instantiate(sentenceMarkerRectanglePrefab, sentenceMarkerRectangleContainer);
            sentenceToEditorUiSentenceMap[sentence] = uiSentence;

            injector.InjectAllComponentsInChildren(uiSentence);
            uiSentence.Init(sentence);
        }

        string label = (sentenceIndex + 1).ToString();
        uiSentence.SetText(label);

        // Update color
        if (sentence.Voice != null)
        {
            Color color = songEditorSceneController.GetColorForVoice(sentence.Voice);
            uiSentence.SetColor(color);

            // Make sentence rectangles alternating light/dark
            bool isDark = (sentenceIndex % 2) == 0;
            if (isDark)
            {
                uiSentence.backgroundImage.MultiplyColor(0.66f);
            }
        }

        PositionUiSentence(uiSentence.RectTransform, xStartPercent, xEndPercent);
    }

    private void PositionUiSentence(RectTransform uiSentenceRectTransform, float xStartPercent, float xEndPercent)
    {
        uiSentenceRectTransform.anchorMin = new Vector2(xStartPercent, 0);
        uiSentenceRectTransform.anchorMax = new Vector2(xEndPercent, 1);
        uiSentenceRectTransform.anchoredPosition = Vector2.zero;
        uiSentenceRectTransform.sizeDelta = Vector2.zero;
    }

    private void CreateSentenceMarkerLine(float xPercent, Color color, int yDashOffset)
    {
        if (xPercent < 0 || xPercent > 1)
        {
            return;
        }

        int x = (int)(xPercent * sentenceLinesImage.TextureWidth);

        for (int y = 0; y < sentenceLinesImage.TextureHeight; y++)
        {
            // Make it dashed
            if (((y + yDashOffset) % 40) < 20)
            {
                sentenceLinesImage.SetPixel(x, y, color);
                // Make it 2 pixels wide
                if (x < sentenceLinesImage.TextureWidth - 1)
                {
                    sentenceLinesImage.SetPixel(x + 1, y, color);
                }
            }
        }
    }

    private EditorUiNote UpdateOrCreateUiNote(Note note)
    {
        if (!noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote editorUiNote))
        {
            editorUiNote = Instantiate(notePrefab, noteContainer);
            injector.Inject(editorUiNote);
            injector.Inject(editorUiNote.GetComponent<EditorNoteContextMenuHandler>());
            editorUiNote.Init(note);

            noteToEditorUiNoteMap.Add(note, editorUiNote);
        }
        else
        {
            editorUiNote.SyncWithNote();
        }

        PositionUiNote(editorUiNote.RectTransform, note.MidiNote, note.StartBeat, note.EndBeat);

        return editorUiNote;
    }

    private void PositionUiNote(RectTransform uiNoteRectTransform, int midiNote, int startBeat, int endBeat)
    {
        float y = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        float xStart = (float)noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = (float)noteArea.GetHorizontalPositionForBeat(endBeat);
        float height = noteArea.HeightForSingleNote;

        uiNoteRectTransform.anchorMin = new Vector2(xStart, y - height / 2f);
        uiNoteRectTransform.anchorMax = new Vector2(xEnd, y + height / 2f);
        uiNoteRectTransform.anchoredPosition = Vector2.zero;
        uiNoteRectTransform.sizeDelta = Vector2.zero;
    }
}
