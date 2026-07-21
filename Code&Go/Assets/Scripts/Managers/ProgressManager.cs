using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Localization;
using Xasu.HighLevel;
using UnityEditor;

/// <summary>
/// Manage the progress of the main story 
/// </summary>
public class ProgressManager : MonoBehaviour {

    #region Properties
    private static ProgressManager instance;
    public static ProgressManager Instance {
        get { return instance; }
    }

    [SerializeField] private bool allUnlocked;  // Todo fix it
    public bool AllUnlocked {
        get { return allUnlocked; }
    }

    [SerializeField] private CategoryDataSO[] categoriesDataSO;

    private new string name;
    public string Name {
        get { return name; }
        set { name = value; }
    }
    #endregion

    // Old
    [SerializeField] private TextAsset activeBlocks;
    [SerializeField] private CategoryDataSO levelsCreatedCategory;

    private CategorySaveData[] categoriesData;

    private CategorySaveData currentCategoryData = null;

    private int lastCategoryUnlocked = 0;

    private LevelsCreatedSaveData levelsCreated;
    private List<string> levelsCreatedHash;

    public LocalizedString createdLevelString;

    #region Methods
    private void Awake() {
        Debug.Log("Progress Manager Awake");
        if (!instance) {
            instance = this;
            DontDestroyOnLoad(this);
            Init();
        }
        else {
            Debug.LogWarning("More than 1 Progress Manager created");
            DestroyImmediate(this);
        }
        Debug.Log("Progress Manager Awake Finished");
    }

    private void Init() {
        categoriesData = new CategorySaveData[categoriesDataSO.Length];

        int i = 0;
        foreach (CategoryDataSO categoryDataSO in categoriesDataSO) {

            CategorySaveData data = new CategorySaveData();
            data.levelsData = new int[categoryDataSO.GetTotalLevels()];

            // Create every level without any star
            for (int j = 0; j < data.levelsData.Length; j++) {
                data.levelsData[j] = allUnlocked ? 0 : -1;
            }

            categoriesData[i] = data;
            i++;
        }

        // Unlock the first level
        categoriesData[1].levelsData[0] = 0;

        // Created Levels
        levelsCreated = new LevelsCreatedSaveData();
        levelsCreated.levelsCreated = new string[0];
        levelsCreatedHash = new List<string>();
        levelsCreatedCategory.levels.Clear();
    }

    /// <summary>
    /// Manage the data when a levels is completed
    /// </summary>
    public void LevelCompleted(int starsAchieved) {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;

        // Set the stars for the current level complete
        if (currentCategoryData.levelsData[currentLevelIndex] == -1) currentCategoryData.levelsData[currentLevelIndex] = 0;

        int newStarsAchieved = Mathf.Clamp(starsAchieved - currentCategoryData.levelsData[currentLevelIndex], 0, 3);
        currentCategoryData.levelsData[currentLevelIndex] = currentCategoryData.levelsData[currentLevelIndex] + newStarsAchieved;

        // Unlock the next level
        int nextLevelIndex = currentLevelIndex + 1;
        int categoryIndex = Array.IndexOf(categoriesData, currentCategoryData);
        int nextCatagoryIndex = categoryIndex + 1;

        // If is in the same category
        if (nextLevelIndex < categoriesData[categoryIndex].levelsData.Length) {
            // If is locked
            if (categoriesData[categoryIndex].levelsData[nextLevelIndex] == -1) categoriesData[categoryIndex].levelsData[nextLevelIndex] = 0;
        }
        // If is the next category
        else if (nextCatagoryIndex < categoriesData.Length) {
            // If is locked
            if (categoriesData[nextCatagoryIndex].levelsData[0] == -1) categoriesData[nextCatagoryIndex].levelsData[0] = 0;
        }

        // Tracks
        var levelName = GameManager.Instance.GetCurrentLevelName();
        CompletableTracker.Instance.Completed(levelName, CompletableTracker.CompletableType.Level)
            .WithSuccess(true)
            .WithScoreRaw(starsAchieved);
        var categoryName = categoriesDataSO[categoryIndex].name_id;
        if (currentCategoryData.GetLevelsCompleted() < currentCategoryData.levelsData.Length) {
            CompletableTracker.Instance.Progressed(categoryName, CompletableTracker.CompletableType.Completable, (float)currentCategoryData.GetLevelsCompleted() / categoriesDataSO[categoryIndex].levels.Count);
        }
        else {
            CompletableTracker.Instance.Completed(categoryName, CompletableTracker.CompletableType.Completable)
                .WithSuccess(true)
                .WithScoreRaw(GetCategoryTotalStars(categoryIndex));
        }
        if (GetGameProgress() == 1.0f) {
            CompletableTracker.Instance.Completed("articoding", CompletableTracker.CompletableType.Game)
                .WithSuccess(true)
                .WithScoreRaw(GetTotalStars());
        }
    }
    #endregion


    //Setters
    public void LevelStarted(int categoryIndex, int levelIndex) {
        if (currentCategoryData == null || (categoryIndex >= 0 && categoryIndex < categoriesData.Length && categoriesData[categoryIndex] != currentCategoryData))
            currentCategoryData = categoriesData[categoryIndex];

        Debug.Log("Level index: " + levelIndex);

        if (levelIndex == 0 && !currentCategoryData.completableInitialized) {
            var categoryName = categoriesDataSO[categoryIndex].name_id;
            CompletableTracker.Instance.Initialized(categoryName, CompletableTracker.CompletableType.Completable);
            currentCategoryData.completableInitialized = true;
        }

        var levelName = GameManager.Instance.GetCurrentLevelName();
        AccessibleTracker.Instance.Accessed(levelName);
    }

    public void LevelStarted(CategoryDataSO category, int level) {
        int index = category.index;
        if (index < 0)
            AccessibleTracker.Instance.Accessed(levelsCreatedCategory.levels[level].levelName);
        else {
            LevelStarted(category.index, level);
        }
    }

    //Getters
    //----------

    public CategorySaveData GetCategoryData(int index) {
        return categoriesData[index];
    }

    public int GetTotalStars() {
        int totalStars = 0;
        foreach (CategorySaveData categoryData in categoriesData) {
            totalStars += categoryData.GetCurrentNumStars();
        }
        return totalStars;
    }

    public int GetTotalLevels() {
        int totalLevels = 0;
        foreach (CategorySaveData categoryData in categoriesData) {
            totalLevels += categoryData.GetTotalLevels();
        }
        return totalLevels;
    }

    public int GetTotalLevelsComplete() {
        int totalLevelsComplete = 0;
        foreach (CategorySaveData categoryData in categoriesData) {
            totalLevelsComplete += categoryData.GetLevelsCompleted();
        }
        return totalLevelsComplete;
    }

    public int GetTotalPerfectLevelsComplete() {
        int totalPerfectLevelsComplete = 0;
        foreach (CategorySaveData categoryData in categoriesData) {
            totalPerfectLevelsComplete += categoryData.GetPerfectLevelsCompleted();
        }
        return totalPerfectLevelsComplete;
    }

    public int GetTotalFinishedCategories() {
        int totalFinishedCategories = 0;
        foreach (CategorySaveData categoryData in categoriesData) {
            if (categoryData.IsCategoryFinished()) totalFinishedCategories++;
        }
        return totalFinishedCategories;
    }

    public bool[] GetPerfectFinishedCategories() {
        bool[] perfectCategories = new bool[categoriesData.Length];
        for (int i = 0; i < perfectCategories.Length; i++) {
            perfectCategories[i] = categoriesData[i].IsCategoryPerfectFinished();
        }
        return perfectCategories;
    }



    public int GetLevelStars(int categoryIndex, int level)
    {
        if (categoryIndex >= categoriesData.Length || categoryIndex < 0 || level >= categoriesData[categoryIndex].levelsData.Length || level < 0) return 0;
        return categoriesData[categoryIndex].levelsData[level];
    }

    public int GetLevelStars(CategoryDataSO category, int level) {
        return GetLevelStars(category.index, level);
    }

    public int GetCategoryTotalStars(int categoryIndex) {
        if (categoryIndex >= categoriesData.Length || categoryIndex < 0) return 0;
        return categoriesData[categoryIndex].GetCurrentNumStars();
    }

    public void UserCreatedLevel(string board, string customActiveBlocks, string customInitialState, Texture2D levelIconImage, string levelName, int levelCategory) {
        //si el nivel ya existe no se guarda
        if (levelsCreatedHash.Contains(Hash.ToHash(board, ""))) return;

        int index = levelsCreatedCategory.levels.Count + 1;
        string path = Application.dataPath;
        string directory = Path.Combine(path, "Levels/Boards/0_CreatedLevels/");

        CreateDirectories();

        if (levelName.Trim() == "") levelName = "NivelCreado";

        string filePath = directory + levelName + ".json";
        FileStream file = new FileStream(filePath, FileMode.Create);
        file.Close();
        StreamWriter writer = new StreamWriter(filePath);
        writer.Write(board);
        writer.Close();

        if (!customInitialState.Equals("NaN")) {
            string directoryInitial = Path.Combine(path, "Levels/InitialStates/0_CreatedLevels/");
            string filePathRestrinction = directoryInitial + levelName + ".txt";
            //Creamos el archivo contenedor del estado inicial
            FileStream fileRestriction = new FileStream(filePathRestrinction, FileMode.Create);
            fileRestriction.Close();
            StreamWriter writerRestriction = new StreamWriter(filePathRestrinction);
            writerRestriction.Write(customInitialState);
            writerRestriction.Close();
        }

        if (!customActiveBlocks.Equals("NaN")) { 

            string directoryRestrinction = Path.Combine(path, "Levels/ActiveBlocks/0_CreatedLevels/");
            string filePathRestrinction = directoryRestrinction + levelName + ".json";
            //Creamos el archivo contenedor de las restricciones
            FileStream fileRestriction = new FileStream(filePathRestrinction, FileMode.Create);
            fileRestriction.Close();
            StreamWriter writerRestriction = new StreamWriter(filePathRestrinction);
            writerRestriction.Write(customActiveBlocks);
            writerRestriction.Close();

            //Y lo cargamos como TextAsset para poder añadirlo al Scriptable del nivel creado
            TextAsset customActiveBlocksAssets = (TextAsset)Resources.Load(levelName);

            AddLevelCreated(board, index, customActiveBlocksAssets, levelName, levelCategory);

        } else {
            AddLevelCreated(board, index, activeBlocks, levelName, levelCategory);
        }

        // Si hay un icono de nivel
        if(levelIconImage) {
            Debug.Log("E");

            var Bytes = levelIconImage.EncodeToPNG();
            Destroy(levelIconImage);
            Debug.Log("F");

            File.WriteAllBytes(Application.dataPath + "/Levels/LevelPreviewIcons/0_CreatedLevels/" + levelName + ".png", Bytes);
            Debug.Log("Level Icon Saved!!!");
        }
        else {
            Debug.LogWarning("level saved without icon");
        }
               
        Array.Resize(ref levelsCreated.levelsCreated, levelsCreated.levelsCreated.Length + 1);
        levelsCreated.levelsCreated[levelsCreated.levelsCreated.GetUpperBound(0)] = levelName;

        Debug.Log("Niveles creados: " + levelsCreated.levelsCreated.Length);
        Debug.Log(levelsCreated.levelsCreated[levelsCreated.levelsCreated.Length-1]);
    }

    /// <summary>
    /// Load the created levels
    /// </summary>
    private void LoadLevelsCreated() {

        string path =
#if UNITY_EDITOR
        Application.dataPath;
#else
        Application.persistentDataPath;
#endif

        for (int i = 0; i < levelsCreated.levelsCreated.Length; i++) {
            // Get the level name
            string levelName = levelsCreated.levelsCreated[i];
            Debug.Log("Creating level " + levelName);
            string filePath = Path.Combine(path, "Levels/Boards/0_CreatedLevels/" + levelName + ".json");
            Debug.Log("Looking for " + filePath);

            string directoryRestrinction = Path.Combine(path, "Assets/Levels/Boards/0_CreatedLevels/");
            string filePathRestrinction = directoryRestrinction + levelName + ".json";
            TextAsset activeBlocks = Resources.Load(filePathRestrinction) as TextAsset;

            // Cambiado
            try
            {
                StreamReader reader = new StreamReader(filePath);
                string readerData = reader.ReadToEnd();
                reader.Close();
                AddLevelCreated(readerData, i + 1, activeBlocks, levelName, 0);
            }
            catch
            {
                Debug.Log("El archivo " + filePath + " no existe");
            }
        }

        Debug.Log("Niveles cargados con exito");
    }

    private void AddLevelCreated(string board, int index, TextAsset customActiveBlocks, string levelName, int levelCategory)
    {
        levelsCreatedHash.Add(board);
    }

    //Save and Load
    //----------------
    public ProgressSaveData Save() {
        ProgressSaveData data = new ProgressSaveData();

        Debug.Log("Save data"); // TODO quitar

        data.categoriesInfo = categoriesData;
        data.levelsCreatedData = levelsCreated;
        data.name = name;

        //data.DebugLogCategoriesData(); // Todo quitar
        
        return data;
    }


    public void Load(ProgressSaveData data) {
        name = data.name;
        categoriesData = data.categoriesInfo;
        levelsCreated = data.levelsCreatedData;

        if(levelsCreated.levelsCreated.Length > 0) 
            Debug.Log("Levels Created going to load: " + levelsCreated.levelsCreated.Length + " - " + levelsCreated.levelsCreated[0]);

        LoadLevelsCreated();
        CheckLevelsData();

        //UpdateCategoryCards();

        //data.DebugLogCategoriesData();
    }

    //private void UpdateCategoryCards() {
    //    for (int i = 0; i < categoryCards.Length; i++) {
    //        categoryCards[i].Configure(categoriesData[i]);
    //    }
    //}

    public float GetGameProgress()
    {
        int totalLevels = GetTotalLevels();
        if (totalLevels == 0) return 0f;
        return (float)GetTotalLevelsComplete() / totalLevels;
    }

    //public int GetTotalStars()
    //{
    //    int stars = 0;

    //    //for (int i = 0; i < categories.Count; i++)
    //    //{
    //    //    stars += (int)GetCategoryTotalStars(i);
    //    //}

    //    return stars;
    //}

    private void CheckLevelsData()
    {
        if (allUnlocked) return;

        foreach(CategorySaveData categoryData in categoriesData)
        {
            for (int i = 0; i < categoryData.levelsData.Length; i++)
            {
                //if (categoryData.lastLevelUnlocked <= i )
                //    categoryData.levelsData[i].stars = -1;
            }
        }
    }

    private void CreateDirectories() {
        Debug.Log("Create levels directories");

        string path = Application.dataPath;

        // Create the Levels directory
        if (!Directory.Exists(path + "/Levels/"))
            Directory.CreateDirectory(path + "/Levels/");

        // Create the ActiveBlocks directory
        if (!Directory.Exists(path + "/Levels/ActiveBlocks/"))
            Directory.CreateDirectory(path + "/Levels/ActiveBlocks/");
        if (!Directory.Exists(path + "/Levels/ActiveBlocks/0_CreatedLevels/"))
            Directory.CreateDirectory(path + "/Levels/ActiveBlocks/0_CreatedLevels/");

        // Create the Boards directory
        if (!Directory.Exists(path + "/Levels/Boards/"))
            Directory.CreateDirectory(path + "/Levels/Boards/");
        if (!Directory.Exists(path + "/Levels/Boards/0_CreatedLevels/"))
            Directory.CreateDirectory(path + "/Levels/Boards/0_CreatedLevels/");

        // Create the InitialStates directory
        if (!Directory.Exists(path + "/Levels/InitialStates/"))
            Directory.CreateDirectory(path + "/Levels/InitialStates/");
        if (!Directory.Exists(path + "/Levels/InitialStates/0_CreatedLevels/"))
            Directory.CreateDirectory(path + "/Levels/InitialStates/0_CreatedLevels/");

        // Create the LevelPreviewIcons directory
        if (!Directory.Exists(path + "/Levels/LevelPreviewIcons/"))
            Directory.CreateDirectory(path + "/Levels/LevelPreviewIcons/");
        if (!Directory.Exists(path + "/Levels/LevelPreviewIcons/0_CreatedLevels/"))
            Directory.CreateDirectory(path + "/Levels/LevelPreviewIcons/0_CreatedLevels/");
    }
}
