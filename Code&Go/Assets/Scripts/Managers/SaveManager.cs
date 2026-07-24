using System.Collections;
using System.Collections.Generic;
using System.IO;
using Simva;
using UnityEngine;

/// <summary>
/// Manage the save and load data
/// </summary>
public class SaveManager : MonoBehaviour {

    #region Properties
    public static SaveManager instance;
    static public SaveManager Instance {
        get { return instance; }
    }

    [SerializeField] private string filename = "gameSave.json";

    private string Filepath = "";
    #endregion

    #region Methods
    public void Awake() {
        Debug.Log("Save Manager Awake");
        if (!instance) {
            instance = this;
            DontDestroyOnLoad(this);
            Init();
        }
        else {
            Debug.LogWarning("More than 1 Save Manager created");
            DestroyImmediate(gameObject);
        }
        Debug.Log("Save Manager Awake Finished");
    }

    /// <summary>
    /// Create the Filepath of the save file
    /// </summary>
    public void Init() {
        string dataPath =
#if UNITY_EDITOR
        Application.dataPath;
#else
        Application.persistentDataPath;
#endif
        string token = "";

        try {
            token = Simva.SimvaManager.Instance.API.Authorization.Agent.account.name;
            token += "_";
        }
        catch (System.Exception e) {
            Debug.LogWarning("Simva.SimvaPlugin: " + e.Message);
            token = "";
        }

        Filepath = Path.Combine(dataPath, token + filename);
    }

    public void Load() {
        Debug.Log("Save Manager - Load data");

        // Si no existe, se crea
        if (!File.Exists(Filepath)) {
            Debug.LogWarning("File not found, creating a new one");
            FileStream file = new FileStream(Filepath, FileMode.Create);
            file.Close();
            Save();
            return;
        }
        else Debug.Log("File found");
        
        // Read the data
        StreamReader reader = new StreamReader(Filepath);
        string readerData = reader.ReadToEnd();
        reader.Close();

        // Create the data from the json
        SaveData data = JsonUtility.FromJson<SaveData>(readerData);

        if (data?.gameData?.progressData != null && data?.gameData?.tutorialInfo != null)
        {
            if (Hash.ToHash(JsonUtility.ToJson(data.gameData), "") == data.hash)
            {
                Debug.Log("Hash coincidente");
                ProgressManager.Instance.Load(data.gameData.progressData);
                Debug.Log("Progress data loaded");
                TutorialManager.Instance.Load(data.gameData.tutorialInfo);
                Debug.Log("Tutorial data loaded");
            }
            else
            {
                Debug.LogWarning("Hash NO coincidente - save file may be corrupted");
            }
        }
        else
        {
            Debug.LogError("Save file is corrupted or empty, starting from defaults");
        }

    }

    public void Save() {
        Debug.Log("Call a save");

        // Save the game data
        GameSaveData gameData = new GameSaveData();
        gameData.progressData = ProgressManager.Instance.Save();
        if (TutorialManager.Instance != null)
            gameData.tutorialInfo = TutorialManager.Instance.Save();
        Debug.Log("Total tutoriales saved: " + gameData.tutorialInfo.tutorials.Length);
        
        // Add the hash to the save data
        SaveData data = new SaveData();
        data.gameData = gameData;
        data.hash = Hash.ToHash(JsonUtility.ToJson(data.gameData), "");

        // Parse to json
        string finalJson = JsonUtility.ToJson(data);

        // Create a new file
        FileStream file = new FileStream(Filepath, FileMode.Create);
        file.Close();

        // Write the new file
        StreamWriter writer = new StreamWriter(Filepath);
        writer.Write(finalJson);
        writer.Close();
    }
    #endregion

}
