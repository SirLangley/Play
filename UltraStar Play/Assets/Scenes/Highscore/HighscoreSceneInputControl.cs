﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using EventSystem = UnityEngine.EventSystems.EventSystem;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneInputControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Button continueButton;
    
    [InjectedInInspector]
    public Button nextDifficultyButton;
    
    [Inject]
    private HighscoreSceneControl highscoreSceneControl;
    
    [Inject]
    private EventSystem eventSystem;
    
    private void Start()
    {
        // Custom navigation implementation in this scene
        eventSystem.sendNavigationEvents = false;
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(context => OnNavigate(context));
        
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();
        if (direction.x > 0)
        {
            highscoreSceneControl.ShowNextDifficulty(1);
        }
        if (direction.x < 0)
        {
            highscoreSceneControl.ShowNextDifficulty(-1);
        }

        if (direction.y != 0)
        {
            // Toggle between buttons
            if (eventSystem.currentSelectedGameObject == nextDifficultyButton.gameObject)
            {
                continueButton.Select();
                return;
            }
            nextDifficultyButton.Select();
        }
    }
}
