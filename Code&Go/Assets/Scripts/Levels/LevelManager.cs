using System;
using System.Xml.Linq;
using System.Xml;
using Xasu.HighLevel;
using System.Collections;
using System.Collections.Generic;
using UBlockly;
using UBlockly.UGUI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Components;
using Input = UnityEngine.Input;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class LevelManager : MonoBehaviour {
    [System.Serializable]
    public struct CategoryTutorialsData
    {
        public PopUpData data;
        public string categoryName;
    }

    private CategoryDataSO currentCategory;
    [SerializeField] private LevelDataSO currentLevel;
    private int currentLevelIndex = 0;

    [SerializeField] private bool buildLimits = true;

    [Space]
    [SerializeField] private StatementManager statementManager;

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFit cameraFit;
    [SerializeField] private CodeZoom codeZoom;

    [SerializeField] private CategoryDataSO defaultCategory;
    [SerializeField] private int defaultLevelIndex;

    [SerializeField] private Text levelName;
    [SerializeField] private LocalizeStringEvent levelNameLocalized;

    [SerializeField] private GameObject saveButton;

    [SerializeField] private CategoryTutorialsData[] categoriesTutorials;

    [SerializeField] private int pasosOffset = 0;

    [SerializeField] private ActivatedScript activatedScript;

    public GameObject endPanel;
    public GameObject transparentRect;
    public GameObject blackRect;
    public GameObject exitConfirmationPanel;

    public GameObject endPanelMinimized;
    public GameObject debugPanel;

    public GameObject gameOverPanel;
    public GameObject gameOverMinimized;

    public StarsController starsController;

    [SerializeField] private PenguinFinishController penguinFinishController;

    private int minimosPasos = 0;
    private string specialBlock = "";
    public LocalizeStringEvent endTextLocalized;
    //[SerializeField] private LocalizeStringEvent endText;

    public StreamRoom streamRoom;
    public Button exitButtonLeft;
    public Button exitButtonRight;
    public Button retryButton;
    public Button guideButton;
    public Button runButton;
    public Button stopButton;

    private bool completed = false;

    private void Awake() {
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null) {
            currentCategory = gameManager.GetCategoryByIndex(gameManager.CurrentCategoryIndex);
            currentLevelIndex = gameManager.CurrentLevelIndex;
        }
        else
        {
            currentCategory = defaultCategory;
            currentLevelIndex = defaultLevelIndex;
        }

        if (!gameManager.IsPlayingCommunityLevel()) {
            currentLevel = currentCategory.levels[currentLevelIndex];
            minimosPasos = currentLevel.minimosPasos;
        }
        else {
            Debug.Log("Community Level");
        }
        
        endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);

#if UNITY_EDITOR
        saveButton.SetActive(true);
#endif
    }

    private void Start() {

        Debug.Log("Start LevelManager");
        //Si estamos jugando desde la pestaña de comunidad, inicializamos de otra forma
        if (GameManager.Instance.IsPlayingCommunityLevel()) InitializeCommunityLevel();
        else Initialize();

        var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
        string text = UBlockly.Xml.DomToText(dom);
        text = GameManager.Instance.ChangeCodeIDs(text);

        CompletableTracker.Instance.Initialized(GameManager.Instance.GetCurrentLevelName().ToLower(), CompletableTracker.CompletableType.Level)
            .WithResultExtension("articoding://ext/code", "\r\n" + text);

        if (currentLevel != null) {
            levelName.text = currentLevel.levelName;
            levelNameLocalized.StringReference = currentLevel.levelNameLocalized;
            levelNameLocalized.RefreshString();

            endTextLocalized.StringReference = currentLevel.endTextLocalized;
            endTextLocalized.RefreshString();
        }
        //Si esta a null es nivel de comunidad 
        else
            //levelName.text = "Comunidad";
            levelName.text = GameManager.Instance.LevelName;
    }

    private void Update()
    {
        if (boardManager == null)
            return;

        if (boardManager.BoardCompleted() && !endPanel.activeSelf && !endPanelMinimized.activeSelf)
        {
            string levelName = GameManager.Instance.GetCurrentLevelName();
            streamRoom.FinishLevel();

            // Active the stars
            starsController.GiveMinimumStepsStar(boardManager.GetCurrentSteps() - (minimosPasos + pasosOffset));
            starsController.GiveHangingCodeStar(boardManager.GetNumOfTopBlocksUsed());

            Debug.Log("Special block after finish: " + specialBlock);
            starsController.GiveSpecialStar(specialBlock);

            // Choose the current penguin reaction
            penguinFinishController.SetFinishAnimation(starsController.GetStars());
            
            endPanel.SetActive(true);
            blackRect.SetActive(true);

            // If is a community-class level
            if (GameManager.Instance.isClassLevel) {
                Debug.Log("Nivel de la comunidad completado");
                string path = "classes/" + GameManager.Instance.currentClassID + "/levels_completed/" + GameManager.Instance.currentLevelID;
                activatedScript.Post(path, JsonUtility.ToJson(""), UpdateClassLevelStateOK, UpdateClassLevelStateKO);
                GameManager.Instance.isClassLevel = false;  // TODO quitar de aqui, se puede salir del nivel sin pasarselo
            }

            if (!GameManager.Instance.IsCreatedLevel()) {
                ProgressManager.Instance.LevelCompleted(starsController.GetStars());

                var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
                string text = UBlockly.Xml.DomToText(dom);
                text = GameManager.Instance.ChangeCodeIDs(text);

                // setVar extensions are folded into LevelCompleted's Completed call inside ProgressManager
                // Store them for ProgressManager to pick up via a local helper approach:
                // Instead, emit a supplementary trace here for the code/steps/star data
                GameObjectTracker.Instance.Used("level_completed_data")
                    .WithResultExtension("articoding://ext/steps", boardManager.GetCurrentSteps())
                    .WithResultExtension("articoding://ext/special_block", starsController.IsSpecialBlockStarActive())
                    .WithResultExtension("articoding://ext/minimum_steps", starsController.IsMinimumStepsStarActive())
                    .WithResultExtension("articoding://ext/no_hanging_code", starsController.IsNoHangingCodeStarActive())
                    .WithResultExtension("articoding://ext/code", "\r\n" + text);
            }
            else
                CompletableTracker.Instance.Completed(levelName, CompletableTracker.CompletableType.Level)
                    .WithSuccess(true)
                    .WithScoreRaw(-1);

            completed = true;
        }

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.N))
            LoadNextLevel();
#endif
    }

    public int UpdateClassLevelStateOK(UnityWebRequest req) {
        Debug.Log("UpdateClassLevelStateOK");
        return 0;
    }

    public int UpdateClassLevelStateKO(UnityWebRequest req) {
        Debug.Log("UpdateClassLevelStateKO");
        return 0;
    }

    private void Initialize() {
        Debug.Log("Initialize() (Not community level)");

        if (currentLevel == null) {
            Debug.LogError("Cannot initialize Level. CurrentLevel is null");
            return;
        }

        // Maybe do more stuff
        ActivateLevelBlocks(currentLevel.activeBlocks, currentLevel.allActive);

        if (currentLevel.initialState == null) LoadInitialBlocks(currentLevel.customInitialState.text);
        else LoadInitialBlocks(currentLevel.initialState);//UI

        string boardJson = currentLevel.levelBoard != null ? currentLevel.levelBoard.text : currentLevel.auxLevelBoard;
        BoardState state = BoardState.FromJson(boardJson);
        boardManager.LoadBoard(state, buildLimits);
        cameraFit.FitBoard(boardManager.GetRows(), boardManager.GetColumns());

        BlocklyUI.WorkspaceView.InitIDs();
    }

    private void InitializeCommunityLevel() {
        // Restricciones y estado inicial
        ActivateLevelBlocks(GameManager.Instance.GetCommunityLevelActiveBlocks(), false);
        LoadInitialBlocks(GameManager.Instance.GetCommunityInitialState());//UI

        // Tablero
        BoardState state = GameManager.Instance.GetCommunityLevelBoard();
        boardManager.LoadBoard(state, buildLimits);
        cameraFit.FitBoard(boardManager.GetRows(), boardManager.GetColumns());

        BlocklyUI.WorkspaceView.InitIDs();
    }

    public void LoadLevel(CategoryDataSO category, int levelIndex) {
        currentCategory = category;
        currentLevelIndex = levelIndex;
        LoadLevel(category.levels[levelIndex]);
    }
    private void LoadLevel(LevelDataSO level)
    {
        currentLevel = level;
        Initialize();
    }

    // It is called when the current level is completed
    public void LoadNextLevel()
    {
        GameManager gMng = GameManager.Instance;

        if (GameManager.Instance.IsPlayingCommunityLevel()) {
            LoadMainMenu();
            return;
        }

        int levelSize = currentCategory.levels.Count;
        CategoryDataSO[] categories = gMng.GetCategories();

        // Si habia una estrella de antes, la quitamos
        SetSpecialBlockStarActive(false);
        
        if (++currentLevelIndex < levelSize) {
            gMng.LoadLevel(currentCategory, currentLevelIndex);
        }
        else //Intenta pasar a la categoria siguiente
        {
            int currentCategoryIndex = currentCategory.index;
            if (++currentCategoryIndex < categories.Length - 1)
            {
                currentCategory = categories[currentCategoryIndex];
                currentLevelIndex = 0;
                gMng.LoadLevel(currentCategory, currentLevelIndex);
            }
            else
            {
                LoadMainMenu();
            }
        }
    }

    public void RestartLevel() {
        codeZoom.ResetZoom();
        if (GameManager.Instance.IsPlayingCommunityLevel()) {
            InitializeCommunityLevel();
        }
        else { 
            LoadInitialBlocks(currentLevel.initialState);
            ActivateLevelBlocks(currentLevel.activeBlocks, currentLevel.allActive);
        }
    }

    public void RetryLevel() {
        ResetLevel();
        gameOverPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        gameOverMinimized.SetActive(false);
        EnableHeaderButtons();

        streamRoom.Retry();

        GameObjectTracker.Instance.Interacted("retry_button");

        var levelName = GameManager.Instance.GetCurrentLevelName();
        CompletableTracker.Instance.Initialized(levelName, CompletableTracker.CompletableType.Level);
    }

    public void ClickStopButton() {
        ResetLevel();
        gameOverPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        gameOverMinimized.SetActive(false);
        EnableHeaderButtons();

        streamRoom.Retry();
    }

    public void MinimizeEndPanel() {
        endPanelMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        GameObjectTracker.Instance.Interacted("end_panel_minimized_button");
    }

    public void MinimizeGameOverPanel() {
        gameOverMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        //endPanel.SetActive(false);
        transparentRect.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        GameObjectTracker.Instance.Interacted("game_over_panel_minimized_button");
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
        retryButton.enabled = false;
        guideButton.enabled = false;
        runButton.enabled = false;
        stopButton.enabled = true;
        stopButton.gameObject.SetActive(true);
    }

    void EnableHeaderButtons()
    {
        exitButtonLeft.enabled = true;
        exitButtonRight.enabled = true;
        retryButton.enabled = true;
        guideButton.enabled = true;
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

    public void ResetLevel()
    {
        boardManager.Reset();
        string boardJson = currentLevel.levelBoard != null ? currentLevel.levelBoard.text : currentLevel.auxLevelBoard;
        BoardState state = BoardState.FromJson(boardJson);
        if (GameManager.Instance.IsPlayingCommunityLevel()) state = GameManager.Instance.GetCommunityLevelBoard();
        boardManager.GenerateBoardElements(state);
        debugPanel.SetActive(true);
        cameraFit.FitBoard(boardManager.GetRows(), boardManager.GetColumns());
        starsController.ReactivateMinimumStepsStar();
    }

    public void ReloadLevel() //TODO: mirar si se usa en algun sitio (lo dudo)
    {
        LoadLevel(currentLevel);
    }

    public void LoadMainMenu() {
        SaveManager.Instance.Save();

        string levelName = GameManager.Instance.GetCurrentLevelName().ToLower();
        GameObjectTracker.Instance.Interacted("level_exit_button")
            .WithResultExtension("articoding://ext/steps", boardManager.GetCurrentSteps())
            .WithResultExtension("articoding://ext/level", levelName);

        var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
        string text = UBlockly.Xml.DomToText(dom);
        text = GameManager.Instance.ChangeCodeIDs(text);

        // Si habia una estrella de antes, la quitamos
        SetSpecialBlockStarActive(false);
        
        if (!completed) {
            CompletableTracker.Instance.Completed(levelName, CompletableTracker.CompletableType.Level)
                .WithSuccess(false)
                .WithScoreRaw(-1f)
                .WithResultExtension("articoding://ext/code", "\r\n" + text);
        }

        if(LoadManager.Instance == null) {
            SceneManager.LoadScene("MenuScene");
            return;
        }

        LoadManager.Instance.LoadScene("MenuScene");
    }

    public void LoadInitialBlocks(LocalizedAsset<TextAsset> textAsset) {
        if (textAsset == null) return;

        StartCoroutine(AsyncLoadInitialBlocks(textAsset));
    }

    public void LoadInitialBlocks(string textAsset) {
        if (textAsset == null) return;

        StartCoroutine(AsyncLoadInitialBlocks(textAsset));
    }

    IEnumerator AsyncLoadInitialBlocks(LocalizedAsset<TextAsset> textAsset) {
        var loadingOp = textAsset.LoadAssetAsync();
        if (!loadingOp.IsDone)
            yield return loadingOp;

        TextAsset localizedTextAsset = loadingOp.Result;
        if (localizedTextAsset == null) yield break;

        BlocklyUI.WorkspaceView.CleanViews();

        var dom = UBlockly.Xml.TextToDom(localizedTextAsset.text);

        UBlockly.Xml.DomToWorkspace(dom, BlocklyUI.WorkspaceView.Workspace);
        BlocklyUI.WorkspaceView.BuildViews();

        yield return null;
    }

    IEnumerator AsyncLoadInitialBlocks(string textAsset) {
        BlocklyUI.WorkspaceView.CleanViews();

        var dom = UBlockly.Xml.TextToDom(textAsset);
        UBlockly.Xml.DomToWorkspace(dom, BlocklyUI.WorkspaceView.Workspace);
        BlocklyUI.WorkspaceView.BuildViews();

        yield return null;
    }

    public void ActivateLevelBlocks(TextAsset textAsset, bool allActive) {
        if (textAsset == null) return;

        StartCoroutine(AsyncActivateLevelBlocks(textAsset, allActive));
    }

    public void ActivateLevelBlocks(ActiveBlocks blocks, bool allActive)
    {
        if (blocks == null) return;

        StartCoroutine(AsyncActivateLevelBlocks(blocks, allActive));
    }

    IEnumerator AsyncActivateLevelBlocks(TextAsset textAsset, bool allActive)
    {
        if (allActive) BlocklyUI.WorkspaceView.Toolbox.SetActiveAllBlocks();
        else if (textAsset != null)
        {
            ActiveBlocks blocks = ActiveBlocks.FromJson(textAsset.text);
            if(blocks.specialBlock == null)
            {
                specialBlock = "None";
            }
            else
            {
                // Ponemos la nueva estrella
                specialBlock = blocks.specialBlock;
                SetSpecialBlockStarActive(true);
            }
            
            BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap(), CategoriesTutorialsAsMap());
        }

        yield return null;
    }

    IEnumerator AsyncActivateLevelBlocks(ActiveBlocks blocks, bool allActive)
    {
        if (allActive) BlocklyUI.WorkspaceView.Toolbox.SetActiveAllBlocks();
        else if (blocks != null)
        {
            if (blocks.specialBlock == null)
            {
                specialBlock = "None";
            }
            else
            {
                // Ponemos la nueva estrella
                specialBlock = blocks.specialBlock;
                SetSpecialBlockStarActive(true);
            }

            BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap(), CategoriesTutorialsAsMap());
        }

        yield return null;
    }

    public Dictionary<string, PopUpData> CategoriesTutorialsAsMap()
    {
        Dictionary<string, PopUpData> map = new Dictionary<string, PopUpData>();
        foreach (CategoryTutorialsData c in categoriesTutorials)//For each category       
            map[c.categoryName.ToUpper()] = c.data;
        return map;
    }

    private void SetSpecialBlockStarActive(bool active)
    {
        Debug.Log("Set special block star: " + specialBlock);
        GameObject blockPrefab = BlockResMgr.Get().LoadBlockViewPrefab(specialBlock);

        if (blockPrefab != null) {
            //Debug.Log(blockPrefab.name);
            ////Debug.Log(blockPrefab.transform);
            //GameObject starP = Instantiate(specialStarPref, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            //starP.transform.parent = blockPrefab.transform;
            ////star.name = "Special Star";
            ////star.transform.SetParent(blockPrefab.transform);

            Transform star = Array.Find(blockPrefab.GetComponentsInChildren<Transform>(),
                (transform => { return transform.gameObject.name == "Star"; }));

            if (star)
                star.gameObject.GetComponent<Image>().enabled = active;
            else Debug.Log("Star is null");
        }
        else Debug.Log("Special block is null");
        
    }
}
