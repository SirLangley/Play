﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NewVersionDialog : INeedInjection, IInjectionFinishedListener, ITranslator
{
    [Inject(Key = R.UxmlNames.dialogTitleHashed)]
    private Label dialogTitle;

    [Inject(Key = R.UxmlNames.dialogMessageHashed)]
    private Label dialogMessage;

    [Inject(Key = R.UxmlNames.dialogCloseButtonHashed)]
    private Button closeButton;

    [Inject(Key = R.UxmlNames.ignoreThisVersionButtonHashed)]
    private Button ignoreThisVersionButton;

    [Inject(Key = R.UxmlNames.ignoreAllFutureVersionsButtonHashed)]
    private Button ignoreAllFutureVersionsButton;

    [Inject]
    private Settings settings;

    private readonly string remoteRelease;
    private readonly string websiteLink;
    private readonly string releaseName;

    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    public NewVersionDialog(VisualElement dialogRootVisualElement,
        VisualElement parentVisualElement,
        Dictionary<string, string> remoteVersionProperties)
    {
        this.dialogRootVisualElement = dialogRootVisualElement;
        this.parentVisualElement = parentVisualElement;
        remoteVersionProperties.TryGetValue("release", out remoteRelease);
        remoteVersionProperties.TryGetValue("name", out releaseName);
        remoteVersionProperties.TryGetValue("website_link", out websiteLink);
    }

    public void OnInjectionFinished()
    {
        // Add callbacks to buttons
        ignoreThisVersionButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.IgnoredReleases.AddIfNotContains(remoteRelease);
            CloseDialog();
        });

        ignoreAllFutureVersionsButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.IgnoredReleases.Clear();
            settings.IgnoredReleases.Add("all");
            CloseDialog();
        });

        closeButton.RegisterCallbackButtonTriggered(() =>
        {
            CloseDialog();
        });

        UpdateTranslation();

        parentVisualElement.Add(dialogRootVisualElement);
    }

    public void CloseDialog()
    {
        parentVisualElement.Remove(dialogRootVisualElement);
    }

    public void UpdateTranslation()
    {
        string displayName = releaseName.IsNullOrEmpty() ? remoteRelease : releaseName;
        dialogMessage.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_message, "remoteRelease", displayName, "websiteLink", websiteLink);
        dialogTitle.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_title);
        ignoreThisVersionButton.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_ignoreThisVersion);
        ignoreAllFutureVersionsButton.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_ignoreAllFutureVersions);
    }
}
