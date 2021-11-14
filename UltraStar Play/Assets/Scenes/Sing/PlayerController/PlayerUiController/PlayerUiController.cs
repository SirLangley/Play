﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiController : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection, IInjectionFinishedListener
{
    public int lineCount = 10;

    [Inject]
    private PlayerScoreController playerScoreController;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private TotalScoreDisplayer totalScoreDisplayer;
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private PlayerMessageDisplayer playerMessageDisplayer;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private SentenceRatingDisplayer sentenceRatingDisplayer;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private PlayerNameText playerNameText;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AvatarImage avatarImage;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerCrownDisplayer PlayerCrownDisplayer { get; private set; }

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private ISingSceneNoteDisplayer noteDisplayer;

    void Start()
    {
        // Show rating and score after each sentence
        ShowTotalScore(playerScoreController.TotalScore);
        playerScoreController.SentenceScoreEventStream.Subscribe(sentenceScoreEvent =>
        {
            ShowTotalScore(playerScoreController.TotalScore);
            ShowSentenceRating(sentenceScoreEvent.SentenceRating);
        });

        // Show an effect for perfectly sung notes
        playerScoreController.NoteScoreEventStream.Subscribe(noteScoreEvent =>
        {
            if (noteScoreEvent.NoteScore.IsPerfect)
            {
                CreatePerfectNoteEffect(noteScoreEvent.NoteScore.Note);
            }
        });

        // Show message when mic (dis)connects.
        if (micProfile != null && micProfile.IsInputFromConnectedClient)
        {
            serverSideConnectRequestManager.ClientConnectedEventStream
                .Subscribe(HandleClientConnectedEvent)
                .AddTo(gameObject);
        }

        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        playerScoreController.SentenceScoreEventStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.AllMatch(x => x.SentenceRating == SentenceRating.Perfect))
            // Create an effect for these.
            .Subscribe(xs => CreatePerfectSentenceEffect());
    }

    private void HandleClientConnectedEvent(ClientConnectionEvent connectionEvent)
    {
        if (micProfile == null
            || connectionEvent.ConnectedClientHandler.ClientId != micProfile.ConnectedClientId)
        {
            return;
        }
        
        if (connectionEvent.IsConnected)
        {
            playerMessageDisplayer.ShowMessage("Mic reconnected", Colors.green, 3);
        }
        else
        {
            playerMessageDisplayer.ShowMessage("Mic disconnected", Colors.red, 3);
        }
    }

    public void OnInjectionFinished()
    {
        InitNoteDisplayer(lineCount);
    }

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        sentenceRatingDisplayer.ShowSentenceRating(sentenceRating);
    }

    public void ShowTotalScore(int score)
    {
        totalScoreDisplayer.ShowTotalScore(score);
    }

    public void CreatePerfectSentenceEffect()
    {
        noteDisplayer.CreatePerfectSentenceEffect();
    }

    public void CreatePerfectNoteEffect(Note perfectNote)
    {
        noteDisplayer.CreatePerfectNoteEffect(perfectNote);
    }

    private void InitNoteDisplayer(int lineCount)
    {
        // Find a suited note displayer
        if (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.SentenceBySentence)
        {
            noteDisplayer = GetComponentInChildren<SentenceDisplayer>(true);
        }
        else if (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.ScrollingNoteStream)
        {
            noteDisplayer = GetComponentInChildren<ScrollingNoteStreamDisplayer>(true);
        }
        if (noteDisplayer == null)
        {
            throw new UnityException("Did not find a suited ISingSceneNoteDisplayer for ENoteDisplayMode " + settings.GraphicSettings.noteDisplayMode);
        }

        // Enable and initialize the selected note displayer
        noteDisplayer.GetGameObject().SetActive(true);
        injector.InjectAllComponentsInChildren(noteDisplayer.GetGameObject());
        noteDisplayer.Init(lineCount);

        // Disable other note displayers
        foreach (ISingSceneNoteDisplayer singSceneNoteDisplayer in GetComponentsInChildren<ISingSceneNoteDisplayer>())
        {
            if (singSceneNoteDisplayer != noteDisplayer)
            {
                singSceneNoteDisplayer.GetGameObject().SetActive(false);
            }
        }
    }
}
