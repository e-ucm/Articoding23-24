using UnityEngine;
using UnityEngine.Localization.Settings;
using Simva;

public static class SimvaLanguageBridge
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LanguageSelectorController.LanguageSelected += OnLanguageSelected;
    }

    private static void OnLanguageSelected(string simvaCode, string unityCode)
    {
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == unityCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                return;
            }
        }
    }
}
