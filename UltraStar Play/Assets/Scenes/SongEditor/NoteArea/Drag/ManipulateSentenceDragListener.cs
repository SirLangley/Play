using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ManipulateSentenceDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private EditorUiSentence uiSentence;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private EditorUiSentenceDragHandler editorUiSentenceDragHandler;

    private List<Note> selectedNotes = new List<Note>();
    private List<Note> followingNotes = new List<Note>();

    private readonly Dictionary<Note, Note> noteToSnapshotOfNoteMap = new Dictionary<Note, Note>();
    private int linebreakBeatSnapshot;
    private bool isCanceled;

    private DragAction dragAction;
    private enum DragAction
    {
        Move,
        StretchRight
    }

    private enum DragDirection
    {
        Horizontal,
        Vertical
    }

    void Start()
    {
        if (editorUiSentenceDragHandler != null)
        {
            editorUiSentenceDragHandler.AddListener(this);
        }
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        if (dragEvent.GeneralDragEvent.InputButton != PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        isCanceled = false;

        dragAction = GetDragAction(uiSentence, dragEvent);

        selectedNotes = uiSentence.Sentence.Notes.ToList();
        if (settings.SongEditorSettings.AdjustFollowingNotes)
        {
            followingNotes = SongMetaUtils.GetFollowingNotes(songMeta, selectedNotes);
        }
        else
        {
            followingNotes.Clear();
        }

        CreateSnapshot(selectedNotes.Union(followingNotes));
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        switch (dragAction)
        {
            case DragAction.Move:
                DragDirection dragDirection = GetDragDirection(dragEvent);
                MoveNotesHorizontal(dragEvent, selectedNotes, true);
                break;

            case DragAction.StretchRight:
                ChangeLinebreakBeat(dragEvent);
                break;
            default:
                throw new UnityException("Unknown drag action: " + dragAction);
        }

        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        if (noteToSnapshotOfNoteMap.Count > 0)
        {
            // Values have been directly applied to the notes. The snapshot can be cleared.
            noteToSnapshotOfNoteMap.Clear();
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
        }
    }

    public void CancelDrag()
    {
        Debug.Log("CancelDrag");
        isCanceled = true;
        foreach (KeyValuePair<Note, Note> noteAndSnapshotOfNote in noteToSnapshotOfNoteMap)
        {
            Note note = noteAndSnapshotOfNote.Key;
            Note snapshotOfNote = noteAndSnapshotOfNote.Value;
            note.CopyValues(snapshotOfNote);
        }
        noteToSnapshotOfNoteMap.Clear();

        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void CreateSnapshot(IEnumerable<Note> notes)
    {
        noteToSnapshotOfNoteMap.Clear();
        foreach (Note note in notes)
        {
            Note noteClone = note.Clone();
            noteToSnapshotOfNoteMap.Add(note, noteClone);
        }
        linebreakBeatSnapshot = uiSentence.Sentence.LinebreakBeat;
    }

    private DragAction GetDragAction(EditorUiSentence dragStartUiSentence, NoteAreaDragEvent dragEvent)
    {
        if (dragStartUiSentence.IsPositionOverRightHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
        {
            return DragAction.StretchRight;
        }
        return DragAction.Move;
    }

    private DragDirection GetDragDirection(NoteAreaDragEvent dragEvent)
    {
        if (Math.Abs(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.y) > Math.Abs(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.x))
        {
            return DragDirection.Vertical;
        }
        return DragDirection.Horizontal;
    }

    private void MoveNotesHorizontal(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
            int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
            note.SetStartAndEndBeat(newStartBeat, newEndBeat);
        }

        if (adjustFollowingNotesIfNeeded && settings.SongEditorSettings.AdjustFollowingNotes)
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }

        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    private void ChangeLinebreakBeat(NoteAreaDragEvent dragEvent)
    {
        uiSentence.Sentence.SetLinebreakBeat(linebreakBeatSnapshot + dragEvent.BeatDistance);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }
}
