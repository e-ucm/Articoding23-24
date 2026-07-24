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
    private string lastLoadedSceneName = null;
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

        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (autoStart && string.IsNullOrEmpty(lastLoadedSceneName))
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            if (!string.IsNullOrEmpty(nextScenePath))
            {
                string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);
                LoadScene(nextSceneName);
            }
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
        if (!string.IsNullOrEmpty(lastLoadedSceneName))
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedSceneName));

        yield return StartCoroutine(WaitUntilLoadingIsComplete());

        lastLoadedSceneName = null;
    }

    public void LoadScene(string sceneName) {
        Debug.Log("Load Scene name: " + sceneName);

        loadingCanvas.SetActive(true);

        // Unload current Scene
        if (!string.IsNullOrEmpty(lastLoadedSceneName))
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedSceneName));

        // Load async 
        loadOperations.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));

        StartCoroutine(WaitUntilLoadingIsComplete());

        lastLoadedSceneName = sceneName;
    }

    public void LoadScene(int index) {
        Debug.Log("Load Scene index: " + index);

        loadingCanvas.SetActive(true);

        // Unload current Scene
        if (!string.IsNullOrEmpty(lastLoadedSceneName))
            loadOperations.Add(SceneManager.UnloadSceneAsync(lastLoadedSceneName));

        // Load async 
        loadOperations.Add(SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive));

        StartCoroutine(WaitUntilLoadingIsComplete());

        string path = SceneUtility.GetScenePathByBuildIndex(index);
        lastLoadedSceneName = System.IO.Path.GetFileNameWithoutExtension(path);
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

        float timeout = Time.realtimeSinceStartup + 10f;
        while (!LocalizationSettings.InitializationOperation.IsDone && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
        }
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            Debug.LogWarning("Localization initialization timed out, continuing without it");
        }
        else if (LocalizationSettings.InitializationOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
        {
            Debug.LogWarning("Localization initialization failed: " + LocalizationSettings.InitializationOperation.OperationException?.Message);
        }

        yield return new WaitForSeconds(extraLoadingTime);

        loadingCanvas.SetActive(false);

        if(!loadingText.gameObject.activeSelf)
            loadingText.gameObject.SetActive(true);
    }
    #endregion

}
