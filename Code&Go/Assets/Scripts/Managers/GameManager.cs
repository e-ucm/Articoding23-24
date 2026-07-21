using Xasu.HighLevel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// Main manager of the game, who creates the SaveManager static class
/// </summary>
public class GameManager : MonoBehaviour {

    #region Properties
    private static GameManager instance;
    static public GameManager Instance {
        get { return instance; }
    }

    [SerializeField] private CategoryDataSO[] categories;
    [SerializeField] private bool tryToLoadSave;

    private int currentCategoryIndex;
    public int CurrentCategoryIndex {
        get { return currentCategoryIndex; }
        set { currentCategoryIndex = value; }
    }

    private int currentLevelIndex;
    public int CurrentLevelIndex {
        get { return currentLevelIndex; }
        set { currentLevelIndex = value; }
    }

    private bool isGameLoaded = false;
    public bool IsGameLoaded {
        get { return isGameLoaded; }
    }

    private Dictionary<UBlockly.Block, string> blockIDs;

    // Variables de conexion
    private bool loggedIn;
    private string token;
    private bool isAdmin = false;
    private string userName = "";
    public int userIconID = 0;
    private bool playingCommunityLevel = false;
    public bool isClassLevel;
    public int currentClassID;
    public int currentLevelID;

    private List<int> likedLevelIDs = new List<int>();
    public List<int> LikedLevelIDs {
        get {
            return likedLevelIDs;
        }
        set {
            likedLevelIDs = value;
        }
    }

    private List<int> likedPlaylistIDs = new List<int>();
    public List<int> LikedPlaylistIDs {
        get {
            return likedPlaylistIDs;
        }
        set {
            likedPlaylistIDs = value;
        }
    }

    BoardState communityBoard = null;
    ActiveBlocks communityActiveBlocks = null;
    string communityInitialState = null;
    private LevelDataSO currentLevel = null;
    public LevelDataSO CurrentLevel {
        get { return currentLevel; }
        set { currentLevel = value; }
    }
    private string levelName;
    public string LevelName {
        get { return levelName; }
        set { levelName = value; }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Create the instance of the singleton, initialize the save manager and destroy the duplicates
    /// </summary>
    private void Awake() {
        Debug.Log("Game Manager Awake");

        if (!instance) {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            Debug.LogWarning("More than 1 Game Manager created");
            DestroyImmediate(gameObject);
        }

        Debug.Log("Game Manager Awake Finished");
    }

    /// <summary>
    /// Just load the game data
    /// </summary>
    private void Start() {
        Debug.Log("Game Manager Start");
        LoadGame();

        CompletableTracker.Instance.Initialized("articoding", CompletableTracker.CompletableType.Game)
            .WithResultExtension("articoding://ext/language", LocalizationSettings.SelectedLocale.Identifier.Code)
            .WithResultExtension("articoding://ext/resolution", Screen.currentResolution.ToString())
            .WithResultExtension("articoding://ext/fullscreen", Screen.fullScreen);
        CompletableTracker.Instance.Progressed("articoding", CompletableTracker.CompletableType.Game, ProgressManager.Instance.GetGameProgress());
        Debug.Log("Game Manager Start Finished");
    }

    /// <summary>
    /// Try to load the game
    /// </summary>
    public void LoadGame() {
        if (tryToLoadSave && !isGameLoaded) {
            SaveManager.Instance.Load();
            isGameLoaded = true;
        }
    }

    /// <summary>
    /// Load an offline level
    /// </summary>
    public void LoadLevel(CategoryDataSO category, int levelIndex) {
        LoadLevel(category.index, levelIndex);
    }

    /// <summary>
    /// Load an offline level
    /// </summary>
    public void LoadLevel(int categoryIndex, int levelIndex) {
        // Set the information of the current level
        blockIDs = new Dictionary<UBlockly.Block, string>();
        playingCommunityLevel = false;
        currentCategoryIndex = categoryIndex;
        currentLevelIndex = levelIndex;

        ProgressManager.Instance.LevelStarted(categories[currentCategoryIndex], currentLevelIndex);

        // Save the data, because you can start a level after finish another
        if (tryToLoadSave) SaveManager.Instance.Save();

        // Load the level scene
        if (LoadManager.Instance == null) {
            SceneManager.LoadScene("LevelScene");
            return;
        }
        LoadManager.Instance.LoadScene("LevelScene");
    }

    /// <summary>
    /// Load a community level
    /// </summary>
    public void LoadCommunityLevel() {
        blockIDs = new Dictionary<UBlockly.Block, string>();
        playingCommunityLevel = true;
        currentCategoryIndex = -1;
        currentLevelIndex = -1;
    }

    /// <summary>
    /// Load the level creator
    /// </summary>
    public void LoadLevelCreator() {
        Debug.Log("Loading level creator");
        blockIDs = new Dictionary<UBlockly.Block, string>();
        currentCategoryIndex = 0;
        currentLevelIndex = -1;

        if (LoadManager.Instance == null) {
            SceneManager.LoadScene("BoardCreation");
            return;
        }
        LoadManager.Instance.LoadScene("BoardCreation");
    }

    /// <summary>
    /// Load 
    /// </summary>
    public void LoadScene(string name) {
        if (LoadManager.Instance == null) {
            SceneManager.LoadScene(name);
            return;
        }
        LoadManager.Instance.LoadScene(name);
    }

    /// <summary>
    /// Save the game when is going to close
    /// </summary>
    public void OnDestroy() {
        if (Instance && tryToLoadSave && SaveManager.Instance)
            SaveManager.Instance.Save();
    }

    /// <summary>
    /// Save the game when is going to close
    /// </summary>
    public void OnApplicationQuit() {
        if (tryToLoadSave && SaveManager.Instance)
            SaveManager.Instance.Save();
    }

    /// <summary>
    /// Get the current level name
    /// </summary>
    public string GetCurrentLevelName() {
        if (currentLevelIndex == -1) return "Editor level";
        else return categories[CurrentCategoryIndex].levels[CurrentLevelIndex].levelName;
    }
    #endregion

    #region Getters And Setters
    public CategoryDataSO GetCategoryByIndex(int index) {
        if (index >= 0 && index < categories.Length) return categories[index];
        else return null;
    }

    public bool IsCreatedLevel() {
        return currentCategoryIndex == -1;
    }

    public CategoryDataSO[] GetCategories()
    {
        return categories;
    }

    private static string GenerateRandomBase58Key(int length)
    {
        var alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, length).Select(_ => alphabet[UnityEngine.Random.Range(0, alphabet.Length)]).ToArray()).ToLower();
    }

    public string GetBlockId(UBlockly.Block block)
    {
        while (!blockIDs.ContainsKey(block))
        {
            var blockId = block.Type + "_" + GenerateRandomBase58Key(4);
            if (!blockIDs.ContainsValue(blockId))
                blockIDs.Add(block, blockId);
        }
        return blockIDs[block];
    }
    
    public string ChangeCodeIDs(string code)
    {
        foreach(var kv in blockIDs)       
            code = code.Replace(kv.Key.ID, kv.Value);
        return code;
    }

    public void SetToken(string newtoken) { token = "Bearer " + newtoken; }
    public string GetToken() { return token; }
    public void SetLogged(bool isLogged) { loggedIn = isLogged; }
    public bool GetLogged() { return loggedIn; }
    public void SetRole(string role) { 
        if (role == "ROLE_USER") isAdmin = false; 
        else isAdmin = true; 
    }
    public void SetAdmin(bool aux) { isAdmin = aux; }
    public bool GetIsAdmin() { return isAdmin; }
    public void SetUserName(string name) { userName = name; }
    public string GetUserName() { return userName; }
    public bool IsPlayingCommunityLevel() { return playingCommunityLevel; }
    public void SetPlayingCommunityLevel(bool aux) { playingCommunityLevel = aux; }
    public void SetCommunityLevelBoard(BoardState state) { communityBoard = state; }
    public BoardState GetCommunityLevelBoard() { return communityBoard; }
    public void SetCommunityLevelActiveBlocks(ActiveBlocks blocks) { communityActiveBlocks = blocks; }
    public ActiveBlocks GetCommunityLevelActiveBlocks() { return communityActiveBlocks; }
    public void SetCommunityInitialState(string state) { communityInitialState = state; }
    public string GetCommunityInitialState() { return communityInitialState; }
    public void ResetCommunityElements() {
        communityInitialState = null;
        communityActiveBlocks = null;
        communityBoard = null;
        playingCommunityLevel = false;
    }
    #endregion

}
