using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private GameObject _errorIcon;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _errorText;
    [SerializeField] private StringTable _stringTableEn;
    [SerializeField] private StringTable _stringTableEs;

    private int _languageCode;
    private void Awake()
    {
        if (LocalizationSettings.SelectedLocale != null && LocalizationSettings.SelectedLocale.Identifier.Code == "en") _languageCode = 0; //English
        else _languageCode = 1; //Espanol
    }

    public void SetLocalizedText(string entry)
    {
        //Elige la tabla correspondiente segun el idioma
        try
        {
            if (_languageCode == 0) {
                _errorText.text = _stringTableEn[entry].LocalizedValue;
            }
            else if(_languageCode == 1) {
                _errorText.text = _stringTableEs[entry].LocalizedValue;
            }
        }
        catch (Exception e)
        {
            _errorIcon.SetActive(false);
            _titleText.gameObject.SetActive(true);
            _errorText.gameObject.SetActive(false);
            Debug.LogError(e.Message);
        }
    }
}
