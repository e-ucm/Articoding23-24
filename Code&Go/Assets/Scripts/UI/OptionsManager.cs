using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Xasu.HighLevel;

public class OptionsManager : MonoBehaviour
{
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    void Awake()
    {
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionDropdown.options.Add(new Dropdown.OptionData(resolutions[resolutions.Length - i - 1].ToString()));
            if(resolutions[resolutions.Length - i - 1].Equals(Screen.currentResolution))
            {
                resolutionDropdown.value = i;
            }
        }
        resolutionDropdown.onValueChanged.AddListener((int value) => OnResolutionDropdownUsed());
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener((bool active) => OnFullscreenToggleUsed());
    }

    IEnumerator Start()
    {
        // Wait for the localization system to initialize
        yield return LocalizationSettings.InitializationOperation;

        // Generate list of available Locales
        var options = new List<Dropdown.OptionData>();
        int selected = 0;
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            if (LocalizationSettings.SelectedLocale == locale)
                selected = i;
            options.Add(new Dropdown.OptionData(locale.name));
        }
        languageDropdown.options = options;

        languageDropdown.value = selected;
        languageDropdown.onValueChanged.AddListener(OnLanguageDropdownUsed);
    }

    public void OnLanguageDropdownUsed(int index)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];

        GameObjectTracker.Instance.Interacted("language_dropdown")
            .WithResultExtension("articoding://ext/language", LocalizationSettings.SelectedLocale.Identifier.Code);
    }

    public void OnResolutionDropdownUsed()
    {
        Resolution res = Screen.resolutions[Screen.resolutions.Length - resolutionDropdown.value - 1];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        GameObjectTracker.Instance.Interacted("resolution_dropdown")
            .WithResultExtension("articoding://ext/resolution", res.ToString());
    }

    public void OnFullscreenToggleUsed()
    {
        Screen.fullScreen = fullscreenToggle.isOn;

        GameObjectTracker.Instance.Interacted("fullscreen_toggle")
            .WithResultExtension("articoding://ext/is_fullscreen", fullscreenToggle.isOn);
    }


}
