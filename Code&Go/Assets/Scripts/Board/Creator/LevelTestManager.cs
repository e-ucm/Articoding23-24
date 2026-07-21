using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Xasu.HighLevel;
using UBlockly.UGUI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LevelTestManager : MonoBehaviour
{
    [SerializeField] CameraFit cameraFit;
    [SerializeField] OrbitCamera cameraOrbit;
    [SerializeField] CameraZoom cameraZoom;
    [SerializeField] Camera levelIconCamera;

    [SerializeField] GameObject levelObjects;
    [SerializeField] GameObject levelCanvas;
    [SerializeField] GameObject levelButtons;
    [SerializeField] RectTransform levelViewport;

    [SerializeField] GameObject creatorObjects;
    [SerializeField] GameObject creatorCanvas;
    [SerializeField] RectTransform creatorViewPort;

    [SerializeField] BoardManager board;
    [SerializeField] BoardCreator boardCreator;
    [SerializeField] TextAsset activeBlocks;

    [SerializeField] GameObject debugPanel;
    [SerializeField] GameObject changeModeButton;
    [SerializeField] Sprite changeToEditModeSprite;
    [SerializeField] Sprite changeToPlayModeSprite;

    [SerializeField] private GameObject saveButton;
    [SerializeField] GameObject loadBoardPanel;
    [SerializeField] GameObject saveBoardPanel;
    [SerializeField] GameObject nameWarningPanel;
    [SerializeField] GameObject confirmSavePanel;
    [SerializeField] GameObject errorSavePanel;

    [SerializeField] StreamRoom streamRoom;

    public GameObject endPanel;
    public GameObject exportButton;
    public GameObject exportUserButton;
    public GameObject logButton;
    public GameObject transparentRect;
    public GameObject blackRect;
    public GameObject endPanelMinimized;
    public GameObject exitConfirmationPanel;

    public RestrictionsPanel restrictionsPanel = new RestrictionsPanel();
    public string levelName = "LevelCreated";
    public int levelCategory = 0;

    public GameObject gameOverPanel;
    public GameObject gameOverMinimized;
    private bool inCreator = false;
    private BoardState initialState;
    private bool completed = false;

    private string initialStateObject = "";

    private string boardString = "";

    [SerializeField] Button resetViewButton;

    public Button exitButtonLeft;
    public Button exitButtonRight;
    public Button modeButton;
    public Button runButton;
    public Button stopButton;

    public Dropdown clasesDropdown;

    public ActivatedScript activatedScript;
    ServerClasses.ClaseJSON clases;

    private bool alreadyInitialized = false;

    private Texture2D levelIconImage; 

    private void Start()
    {
        if (GameManager.Instance.IsPlayingCommunityLevel())
        {
            Invoke("ChangeModeFromEditButton", 0.01f);
        }
        else
        {
            Invoke("ChangeMode", 0.01f);
            ActivateLevelBlocks(activeBlocks, false);
        }
#if UNITY_EDITOR
        loadBoardPanel.SetActive(true);
        saveBoardPanel.SetActive(true);
        saveButton.SetActive(true);
#endif
    }

    private void Update()
    {
        resetViewButton.interactable = !cameraOrbit.IsReset();

        if (BlocklyUI.WorkspaceView != null && alreadyInitialized == false)
        {
            LoadInitialBlocks(GameManager.Instance.GetCommunityInitialState());
            ActivateLevelBlocks(GameManager.Instance.GetCommunityLevelActiveBlocks(), false);
            alreadyInitialized = true;
        }

        if (inCreator)
        {
            bool enabled = board.GetNEmitters() >= board.GetNReceivers() && board.GetNReceivers() > 0 && !board.AllReceiving();
            changeModeButton.GetComponent<Button>().enabled = enabled;
            changeModeButton.GetComponent<Image>().color = enabled ? Color.white : Color.grey;
        }
        else if (board.BoardCompleted() && !completed)
        {
            GameManager.Instance.ResetCommunityElements();

            completed = true;
            endPanel.SetActive(true);

            bool logged = GameManager.Instance.GetLogged();
            logButton.SetActive(!logged);

            if (logged) {
                if (GameManager.Instance.GetIsAdmin())
                {
                    exportButton.SetActive(logged);
                    exportUserButton.SetActive(!logged);
                }
                else
                {
                    exportButton.SetActive(!logged);
                    exportUserButton.SetActive(logged);
                }
            }
            else
            {
                exportButton.SetActive(false);
                exportUserButton.SetActive(false);
            }

            blackRect.SetActive(true);
            streamRoom.FinishLevel();

            string levelNameEditor = GameManager.Instance.GetCurrentLevelName();
            CompletableTracker.Instance.Completed(levelNameEditor, CompletableTracker.CompletableType.Level)
                .WithSuccess(true)
                .WithScoreRaw(-1f)
                .WithResultExtension("articoding://ext/steps", board.GetCurrentSteps());
            
        }
    }

    public void CreateClassesDropdown()
    {
        //Obtenemos la lista de clases del profesor creando el nivel
        if (GameManager.Instance.GetIsAdmin())
        {
            activatedScript.Get("classes", GetClassesOK, GetClassesKO);
        }
    }

    public void TryToSaveLocal()
    {
        SaveLevelLocal();
    }

    public void TryToExport()
    {
        ExportLevel(true);
    }

    public void TryToUserExport()
    {
       ExportLevel(false);
    }

    public void SaveLevelLocal()
    {
        confirmSavePanel.SetActive(true);
        ProgressManager.Instance.UserCreatedLevel(initialState.ToJson(), restrictionsPanel.GetActiveBlocks().ToJson(), initialStateObject, levelIconImage, levelName, 7);
    }

    //Convertirmos los datos que tenemos (nombre, nivel y clase a la que va) a un objeto
    //que postear al servidor
    public void ExportLevel(bool isTeacher) {
        ServerClasses.PostedLevel levelToPost = new ServerClasses.PostedLevel();

        levelToPost.title = levelName;
        levelToPost.classes = new List<int>();
        if(isTeacher)
            levelToPost.classes.Add(clases.content[clasesDropdown.value].id);
        levelToPost.publicLevel = !isTeacher;
        levelToPost.articodingLevel = new ServerClasses.ArticodingLevel();
        ActiveBlocks thisActiveBlocks = ActiveBlocks.FromJson(activeBlocks.text);
        levelToPost.articodingLevel.activeblocks = thisActiveBlocks;
        levelToPost.articodingLevel.boardstate = initialState;
        levelToPost.articodingLevel.initialState = initialStateObject;

        activatedScript.Post("levels", JsonUtility.ToJson(levelToPost), GetPostLevelOK, GetPostLevelKO);
    }

    int GetPostLevelOK(UnityWebRequest req)
    {
        confirmSavePanel.SetActive(true);

        return 0;
    }

    int GetPostLevelKO(UnityWebRequest req)
    {
        errorSavePanel.SetActive(true);

        return 0;
    }

    public void ChangeMode()
    {
        ChangeMode(false);
    }

    /// <summary>
    /// Change to Play Mode with the current created level
    /// </summary>
    public void TryChangeMode()
    {
        // TODO sacar captura del nivel
        TakeLevelIcon();

        ChangeMode(true);
        if (levelName.Trim(' ') == "") nameWarningPanel.SetActive(true);
    }

    private void TakeLevelIcon() {
        Debug.Log("Take Level Icon");
        try {
            // Disable the creator objects
            creatorObjects.SetActive(false);

            // Take the cam
            Camera cam = levelIconCamera;

            // Adjust the camera
            cam.transform.position = new Vector3(board.GetColumns() / 2.0f - 0.5f, 5, board.GetRows() / 2.0f - 0.5f);
            cam.transform.rotation = Camera.main.transform.rotation;
            cam.transform.localScale = Camera.main.transform.localScale;
            int max = Mathf.Max(board.GetColumns(), board.GetRows()) - 2;
            cam.orthographicSize = max / 2.0f + 0.5f;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;
            cam.Render();

            // Save the current level icon image
            levelIconImage = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
            levelIconImage.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
            levelIconImage.Apply();
            RenderTexture.active = currentRT;

            //Debug.Log("E");

            //var Bytes = levelIconImage.EncodeToPNG();
            //Destroy(levelIconImage);
            //Debug.Log("F");

            //File.WriteAllBytes(Application.dataPath + "/AAAAA/" + "Testing.png", Bytes);
            ////File.WriteAllBytes(Application.dataPath + "/Levels/LevelPreviewIcons/0_CreatedLevels/" + "Testing.png", Bytes);
            //Debug.Log("G");

            creatorObjects.SetActive(true);
        } 
        
        catch(System.Exception e) {
            Debug.LogError(e.Message);
        }
    }

    public void ChangeModeFromEditButton()
    {
        inCreator = false;

        initialState = GameManager.Instance.GetCommunityLevelBoard();
        board.LoadBoard(initialState);
        levelName = GameManager.Instance.GetCurrentLevelName();

        levelObjects.SetActive(!inCreator);
        levelCanvas.SetActive(!inCreator);
        levelButtons.SetActive(!inCreator);

        creatorObjects.SetActive(inCreator);
        creatorCanvas.SetActive(inCreator);

        board.SetModifiable(inCreator);

        completed = false;
        cameraFit.SetViewPort(levelViewport);

        changeModeButton.GetComponent<Image>().sprite = changeToEditModeSprite;
        board.SetFocusPointOffset(new Vector3((board.GetColumns() - 2) / 2.0f + 0.5f, 0.0f, (board.GetRows() - 2) / 2.0f + 0.5f));
        cameraFit.FitBoard(board.GetRows(), board.GetColumns());

        string boardState = board.GetBoardStateAsFormatedString();
        boardString = boardState;
    }

    public void ChangeMode(bool fromButton)
    {
        inCreator = !inCreator;

        levelObjects.SetActive(!inCreator);
        levelCanvas.SetActive(!inCreator);
        levelButtons.SetActive(!inCreator);

        creatorObjects.SetActive(inCreator);
        creatorCanvas.SetActive(inCreator);

        board.SetModifiable(inCreator);

        if (!inCreator)
        {
            ActivateLevelBlocks(restrictionsPanel.GetActiveBlocks());
            completed = false;
            initialState = board.GetBoardState();
            cameraFit.SetViewPort(levelViewport);

            changeModeButton.GetComponent<Image>().sprite = changeToEditModeSprite;
            board.SetFocusPointOffset(new Vector3((board.GetColumns() - 2) / 2.0f + 0.5f, 0.0f, (board.GetRows() - 2) / 2.0f + 0.5f));
            cameraFit.FitBoard(board.GetRows(), board.GetColumns());

            string boardState = board.GetBoardStateAsFormatedString();

            if (fromButton)
            {
                GameObjectTracker.Instance.Interacted("editor_mode_change_button")
                    .WithResultExtension("articoding://ext/mode", "test")
                    .WithResultExtension("articoding://ext/board", boardState != boardString ? boardState : "unchanged");
            }
            boardString = boardState;
        }
        else
        {
            cameraOrbit.ResetInmediate();
            cameraZoom.ResetInmediate();
            cameraFit.SetViewPort(creatorViewPort);
            changeModeButton.GetComponent<Image>().sprite = changeToPlayModeSprite;
            boardCreator.FitBoard();

            if (fromButton)
                GameObjectTracker.Instance.Interacted("editor_mode_change_button")
                    .WithResultExtension("articoding://ext/mode", "edition");
        }

        if (inCreator)
            AccessibleTracker.Instance.Accessed("editor");
        else
            AccessibleTracker.Instance.Accessed("tester");
    }

    public void LoadMainMenu()
    {
        // Si estamos en la creacion del tablero todavia no existe el workspace
        if (BlocklyUI.WorkspaceView != null)
        {
            var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
            string text = UBlockly.Xml.DomToText(dom);
            text = GameManager.Instance.ChangeCodeIDs(text);

        if (!completed)
        {
            CompletableTracker.Instance.Completed(levelName, CompletableTracker.CompletableType.Level)
                .WithSuccess(false)
                .WithScoreRaw(-1f)
                .WithResultExtension("articoding://ext/code", "\r\n" + text);
        }
        }

        GameManager.Instance.ResetCommunityElements();

        if (LoadManager.Instance == null)
        {
            SceneManager.LoadScene("MenuScene");
            return;
        }

        LoadManager.Instance.LoadScene("MenuScene");
    }

    public void ResetLevel()
    {
        board.Reset();
        board.GenerateBoardElements(initialState);
        debugPanel.SetActive(true);
        cameraFit.FitBoard(board.GetRows(), board.GetColumns());
        GameObjectTracker.Instance.Interacted("editor_retry_button");

        var levelName = GameManager.Instance.GetCurrentLevelName();
        CompletableTracker.Instance.Initialized(levelName, CompletableTracker.CompletableType.Level);
    }

    public void RetryLevel()
    {
        ResetLevel();
        gameOverPanel.SetActive(false);
        endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        gameOverMinimized.SetActive(false);
        EnableHeaderButtons();

        streamRoom.Retry();
        completed = false;
    }

    public void MinimizeEndPanel()
    {
        endPanelMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        GameObjectTracker.Instance.Used("end_panel_minimized");
    }

    public void MinimizeGameOverPanel()
    {
        gameOverMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        //endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        GameObjectTracker.Instance.Used("game_over_panel_minimized");
    }

    public void SetActiveNoInputPanel()
    {
        transparentRect.SetActive(true);
        DisableHeaderButtons();
    }

    void DisableHeaderButtons()
    {
        exitButtonLeft.enabled = false;
        exitButtonRight.enabled = false;
        modeButton.enabled = false;
        runButton.enabled = false;
        stopButton.enabled = true;
        stopButton.gameObject.SetActive(true);
    }

    void EnableHeaderButtons()
    {
        exitButtonLeft.enabled = true;
        exitButtonRight.enabled = true;
        modeButton.enabled = true;
        runButton.enabled = true;
        stopButton.enabled = false;
        stopButton.gameObject.SetActive(false);
    }

    public void SetActiveExitConfirmationPanel(bool active)
    {
        exitConfirmationPanel.SetActive(active);
        transparentRect.SetActive(false);
        blackRect.SetActive(active);
    }

    public void ActivateLevelBlocks(TextAsset textAsset, bool allActive)
    {
        if (textAsset == null) return;

        StartCoroutine(AsyncActivateLevelBlocks(textAsset, allActive));
    }

    public void ActivateLevelBlocks(ActiveBlocks ablocks)
    {
        ActiveBlocks blocks = ablocks;
        BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap());
    }

    IEnumerator AsyncActivateLevelBlocks(TextAsset textAsset, bool allActive)
    {
        if (allActive) BlocklyUI.WorkspaceView.Toolbox.SetActiveAllBlocks();
        else if (textAsset != null)
        {
            ActiveBlocks blocks = ActiveBlocks.FromJson(textAsset.text);
            BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap());
        }

        yield return null;
    }

    public void ActivateLevelBlocks(ActiveBlocks blocks, bool allActive)
    {
        if (blocks == null) return;

        StartCoroutine(AsyncActivateLevelBlocks(blocks, allActive));
    }

    IEnumerator AsyncActivateLevelBlocks(ActiveBlocks blocks, bool allActive)
    {
        if (allActive) BlocklyUI.WorkspaceView.Toolbox.SetActiveAllBlocks();
        else if (blocks != null)
        {
            BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap());
        }

        yield return null;
    }

    public void LoadInitialBlocks(string textAsset)
    {
        if (textAsset == null) return;

        StartCoroutine(AsyncLoadInitialBlocks(textAsset));
    }

    IEnumerator AsyncLoadInitialBlocks(string textAsset)
    {
        BlocklyUI.WorkspaceView.CleanViews();

        var dom = UBlockly.Xml.TextToDom(textAsset);
        UBlockly.Xml.DomToWorkspace(dom, BlocklyUI.WorkspaceView.Workspace);
        BlocklyUI.WorkspaceView.BuildViews();

        yield return null;
    }

    public void ChangeLevelName(string newLevelName)
    {
        levelName = newLevelName;
    }  

    public void ChangeLevelCategory(int newLevelCategory)
    {
        levelCategory = newLevelCategory;
    }

    public void SaveActualBlocks()
    {
        var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
        string text = UBlockly.Xml.DomToText(dom);

        initialStateObject = text;
    }

    int GetClassesOK(UnityWebRequest req)
    {
        string clasesJson = req.downloadHandler.text;

        try
        {
            clases = JsonUtility.FromJson<ServerClasses.ClaseJSON>(clasesJson);
        }
        catch (System.Exception e)
        {
            Debug.Log("Error al leer clases " + e);
        }

        //Si es profesor, activamos el boton y añadimos las clases a la elección de clases para subir el nivel
        List<ServerClasses.Clase> clasesList = clases.content;

        clasesDropdown.ClearOptions();

        for (int i = 0; i < clasesList.Count; i++)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = clasesList[i].name;
            clasesDropdown.options.Add(data);
        }

        clasesDropdown.RefreshShownValue();

        return 0;
    }

    int GetClassesKO(UnityWebRequest req)
    {
        Debug.Log("Error al obtener clases");
        return 0;
    }

    public void ActivateExportButtons(bool isTeacher)
    {
        exportButton.SetActive(isTeacher);
        exportUserButton.SetActive(!isTeacher);
    }

}
