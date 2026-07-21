using AssetPackage;
using System.Collections;
using System.Collections.Generic;
using Simva;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityFx.Async.Promises;

/// <summary>
/// Manage the functions of the settings and its buttons
/// </summary>
public class SettingsManager : MonoBehaviour {

    // Buttons hidden panels
    [SerializeField] private GameObject blackPanel;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject exitConfirmationPanel;
    [SerializeField] private GameObject optionsMenu;



    private void Start() {
        TrackerAsset.Instance.Accessible.Accessed("menu");

        ResetSettingPanels();
    }

    /// <summary>
    /// Black button panel that hides all setting panels and subpanels
    /// </summary>
    public void ResetSettingPanels() {
        settingsMenu.SetActive(false);
        optionsMenu.SetActive(false);
        exitConfirmationPanel.SetActive(false);
        blackPanel.SetActive(false);
    }

    /// <summary>
    /// Active the settings menu panel
    /// </summary>
    public void ToggleSettingsMenu() {
        blackPanel.SetActive(true);
        settingsMenu.SetActive(!settingsMenu.activeSelf);

        TrackerAsset.Instance.setVar("state", settingsMenu.activeSelf ? "opened" : "closed");
        TrackerAsset.Instance.GameObject.Interacted("settings_button");
    }

    /// <summary>
    /// Post-Test Button function. Opens the test URL.
    /// </summary>
    public void GoToPostSurvey() {
        //Simva.SimvaPlugin.Instance.Instance.NotifyLoading(true);
        string activityId = Simva.SimvaManager.Instance.CurrentActivityId;
        Simva.Model.Schedule schedule = Simva.SimvaManager.Instance.Schedule;
        var dic = schedule.Activities;
        string username = Simva.SimvaManager.Instance.API.Authorization.Agent.account.name;

        foreach (var activity in dic.Values) {
            if (activity.Type == "limesurvey" && activity.Name.ToLower().Contains("post")) {
                activityId = activity.Id;
            }
        }

        Simva.SimvaManager.Instance.API.Api.GetActivityTarget(activityId).Then(
            result => { Application.OpenURL(result[username]); });
    }


    /// <summary>
    /// Active or deactive the options panel
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveOptionsPanel(bool active) {
        optionsMenu.SetActive(active);
        blackPanel.SetActive(active);

        if (active)
            TrackerAsset.Instance.Accessible.Accessed("options_panel", AccessibleTracker.Accessible.Screen);
        else
            TrackerAsset.Instance.GameObject.Interacted("options_panel_close_button");
    }


    /// <summary>
    /// Active or deactive the exit confirmation panel
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveExitConfirmationPanel(bool active) {
        exitConfirmationPanel.SetActive(active);
        blackPanel.SetActive(active);

        if (active)
            TrackerAsset.Instance.Accessible.Accessed("exit_game_panel", AccessibleTracker.Accessible.Screen);
        else
            TrackerAsset.Instance.GameObject.Interacted("exit_game_panel_close_button");
    }

    /// <summary>
    /// Change to the credits scene
    /// </summary>
    public void LoadCreditsScene() {
        .Instance.Accessible.Accessed("credits", AccessibleTracker.Accessible.Screen);

        if (LoadManager.Instance == null) {
            SceneManager.LoadScene("EndScene");
            return;
        }

        LoadManager.Instance.LoadScene("EndScene");
    }

    public void ExitGame() {
        //GameManager.instance.Quit(); //TODO: GameManager
        bool gameCompleted = ProgressManager.Instance.GetGameProgress() == 1f;
        TrackerAsset.Instance.Completable.Completed("articoding", CompletableTracker.Completable.Game, gameCompleted, ProgressManager.Instance.GetTotalStars());

        Simva.SimvaPlugin.Instance.WantsToQuit();
    }

    public void TraceEditor() {
        TrackerAsset.Instance.Accessible.Accessed("editor_levels", AccessibleTracker.Accessible.Screen);
    }
}
