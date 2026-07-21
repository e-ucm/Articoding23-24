using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using Simva;

/// <summary>
/// Manage the loading of the scenes
/// </summary>
public class LoadManager : MonoBehaviour {

    #region Properties
    private static LoadManager instance;
    static public LoadManager Instance {
        get { return instance; }
    }

    [SerializeField] private bool autoStart;
    public bool AutoStart {
        get { return autoStart; }
        set { autoStart = value; }
    }

    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private Text loadingText;
    [SerializeField] private float extraLoadingTime = 1.0f;

    private List<AsyncOperation> loadOperations = new List<AsyncOperation>();
    private int lastLoadedIndex = -1;
    #endregion

    #region Methods
    private void Awake() {
        if (!instance) {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            Debug.LogWarning("More than 1 Load Manager created");
            DestroyImmediate(this);
        }
    }

    private IEnumerator Start() {
        yield return WaitUntilLoadingIsComplete();
        if (autoStart && lastLoadedIndex == -1)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            Scene nextScene = SceneManager.GetSceneByBuildIndex(nextIndex);
            if (!nextScene.IsValid() || !nextScene.isLoaded)
                LoadScene(nextIndex);
        }
    }

    void LateUpdate() {
        if (!loadingCanvas.activeInHierarchy) return;

        Color color = loadingText.color;
        color.a = 1.0f + Mathf.Sin(Time.timeSinceLevelLoad);
        loadingText.color = color;
    }

    public IEnumerator Unload() {
        loadingCanvas.SetActive(true);

        // Unload current Scene
        if (lastLoadedIndex != -1)
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedIndex));

        yield return StartCoroutine(WaitUntilLoadingIsComplete());

        lastLoadedIndex = -1;
    }

    public void LoadScene(string sceneName) {
        Debug.Log("Load Scene name: " + sceneName);

        loadingCanvas.SetActive(true);

        // Unload current Scene
        if (lastLoadedIndex != -1)
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedIndex));

        // Load async 
        loadOperations.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));

        StartCoroutine(WaitUntilLoadingIsComplete());

        lastLoadedIndex = SceneManager.GetSceneByName(sceneName).buildIndex;
    }

    public void LoadScene(int index) {
        Debug.Log("Load Scene index: " + index);

        loadingCanvas.SetActive(true);

        // Unload current Scene
        if (lastLoadedIndex != -1)
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedIndex));

        // Load async 
        loadOperations.Add(SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive));

        StartCoroutine(WaitUntilLoadingIsComplete());

        lastLoadedIndex = index;
    }

    private IEnumerator WaitUntilLoadingIsComplete() {
        // Wait for scene loading operations
        for (int i = 0; i < loadOperations.Count; i++)
        {
            while(!loadOperations[i].isDone)
            {
                yield return null;
            }
        }
        loadOperations.Clear();

        // Wait for localization operations
        while (!LocalizationSettings.InitializationOperation.IsDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(extraLoadingTime);

        loadingCanvas.SetActive(false);

        if(!loadingText.gameObject.activeSelf)
            loadingText.gameObject.SetActive(true);
    }
    #endregion

}
