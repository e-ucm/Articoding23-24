using System.Collections;
using System.Collections.Generic;
using Simva;
using Xasu.HighLevel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityFx.Async.Promises;
using Xasu;

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
        AccessibleTracker.Instance.Accessed("menu");

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

        GameObjectTracker.Instance.Interacted("settings_button")
            .WithResultExtension("articoding://ext/state", settingsMenu.activeSelf ? "opened" : "closed");
    }

    /// <summary>
    /// Active or deactive the options panel
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveOptionsPanel(bool active) {
        optionsMenu.SetActive(active);
        blackPanel.SetActive(active);

        if (active)
            AccessibleTracker.Instance.Accessed("options_panel", AccessibleTracker.AccessibleType.Screen);
        else
            GameObjectTracker.Instance.Interacted("options_panel_close_button");
    }


    /// <summary>
    /// Active or deactive the exit confirmation panel
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveExitConfirmationPanel(bool active) {
        exitConfirmationPanel.SetActive(active);
        blackPanel.SetActive(active);

        if (active)
            AccessibleTracker.Instance.Accessed("exit_game_panel", AccessibleTracker.AccessibleType.Screen);
        else
            GameObjectTracker.Instance.Interacted("exit_game_panel_close_button");
    }

    /// <summary>
    /// Change to the credits scene
    /// </summary>
    public void LoadCreditsScene() {
        AccessibleTracker.Instance.Accessed("credits", AccessibleTracker.AccessibleType.Screen);

        if (LoadManager.Instance == null) {
            SceneManager.LoadScene("EndScene");
            return;
        }

        LoadManager.Instance.LoadScene("EndScene");
    }

    public void ExitGame() {
        bool gameCompleted = ProgressManager.Instance.GetGameProgress() == 1f;
        if (CompletableTracker.IsInitialized("articoding"))
        {
            CompletableTracker.Instance.Completed("articoding", CompletableTracker.CompletableType.Game)
                .WithSuccess(gameCompleted)
                .WithScoreRaw(ProgressManager.Instance.GetTotalStars());
        }

        if (SimvaManager.Instance != null && SimvaManager.Instance.IsActive)
        {
            Simva.SimvaPlugin.Instance.WantsToQuit();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public void TraceEditor() {
        AccessibleTracker.Instance.Accessed("editor_levels", AccessibleTracker.AccessibleType.Screen);
    }
}
