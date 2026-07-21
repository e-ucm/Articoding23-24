using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using Xasu.HighLevel;

public class PopUpManager : MonoBehaviour {
    private static PopUpManager instance;
    public static PopUpManager Instance {
        get { return instance; }
    }

    [SerializeField] private RectTransform bodyRect;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private GameObject mainContent;
    [SerializeField] private PopUp popupPanel;
    [SerializeField] private Image highlightImage;
    [Space]
    [SerializeField] private Shader highlightShader;
    [SerializeField] [Min(0.0f)] private float highlightPadding;
    private Material imageMaterial;

    private void Awake() {
        if (!instance) {
            instance = this;
            DontDestroyOnLoad(gameObject);
            //Init();
            //return;
        }
        else {
            Debug.LogWarning("More than 1 Pop Up Manager created");
            DestroyImmediate(gameObject);
        }
    }

    private void Start() {
        Init();
    }

    private void Init() {
        imageMaterial = new Material(highlightShader);
        highlightImage.material = imageMaterial;
        mainContent.SetActive(false);
        if (TutorialManager.Instance.TutorialsON) bodyRect.gameObject.SetActive(true);
        Hide();
    }

    public void Show(PopUpData data) {
        imageMaterial.SetVector("_PositionSize", Vector4.zero);
        mainContent.SetActive(true);
        popupPanel.Show(data);
        popupPanel.CenterPosition();

        TraceShow(data);
        popupPanel.AddListener(() => { TraceHide(data); Hide(); });
    }


    public void Show(PopUpData data, Rect rect) {
        TraceShow(data);
        float xPadding = highlightPadding * Screen.width / bodyRect.rect.width;
        float yPadding = highlightPadding * Screen.height / bodyRect.rect.height;
        Vector2 position = new Vector2(rect.x + rect.width / 2.0f, rect.y + rect.height / 2.0f);
        Vector2 offset = new Vector2(rect.width / 2.0f + xPadding, rect.height / 2.0f + yPadding);

        imageMaterial.SetVector("_PositionSize", new Vector4(rect.x, rect.y, rect.width, rect.height));

        mainContent.SetActive(true);
        popupPanel.Show(data);
        popupPanel.SetTargetPositionAndOffset(position, offset);

        if (data.next != null)
            popupPanel.AddListener(() => { TraceHide(data); Show(data.next, rect); });
        else
            popupPanel.AddListener(() => { TraceHide(data); Hide(); });

    }

    private void TraceShow(PopUpData data) {
        string content = data.localizedTitle.GetLocalizedStringAsync().Result + ": " + data.localizedContent.GetLocalizedStringAsync().Result;
        CompletableTracker.Instance.Initialized("tip_" + data.name.ToLower(), CompletableTracker.CompletableType.DialogFragment)
            .WithResultExtension("articoding://ext/content", content.Replace("\"", "'"));
    }

    private void TraceHide(PopUpData data) {
        string content = data.localizedTitle.GetLocalizedStringAsync().Result + ": " + data.localizedContent.GetLocalizedStringAsync().Result;
        CompletableTracker.Instance.Completed("tip_" + data.name.ToLower(), CompletableTracker.CompletableType.DialogFragment)
            .WithResultExtension("articoding://ext/content", content.Replace("\"", "'"));
    }

    public void Hide() {
        mainContent.SetActive(false);
        popupPanel.Hide();
    }


    public bool IsShowing() {
        return popupPanel.gameObject.activeSelf && mainContent.activeSelf;
    }

    public GraphicRaycaster GetGraphicRaycaster() {
        return graphicRaycaster;
    }

}
