using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manage the tutorials pop ups
/// </summary>
public class TutorialManager : MonoBehaviour {
    private static TutorialManager instance;
    public static TutorialManager Instance {
        get { return instance; }
    }

    [SerializeField] private bool tutorialsON;
    public bool TutorialsON {
        get { return tutorialsON; }
    }

    [SerializeField] private PopUpManager popUpManager;

    private BinaryHeap<TutorialTrigger> priorTriggers = new BinaryHeap<TutorialTrigger>();
    private List<TutorialTrigger> conditionTriggers = new List<TutorialTrigger>();
    private HashSet<string> triggered = new HashSet<string>();   // Stores the hash of all triggered tutorial
    private List<TutorialTrigger> savePending = new List<TutorialTrigger>();  // Stores the pending triggers to be saved
    private HashSet<string> saved = new HashSet<string>();       // Stores save data, loaded from file or modified on execution

    private bool needToBeDestroyed = false;
    private float lastWidth;
    private float lastHeight;

    private TutorialTrigger lastTutorialTrigger;

    private void Awake() {
        Debug.Log("Tutorial Manager Awake");

        if (!instance) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Debug.LogWarning("More than 1 Tutorial Manager created");
            DestroyImmediate(gameObject);
        }
        Debug.Log("Tutorial Manager Awake Finished");
    }

    private void Update() {
        if (!tutorialsON) return;

        if(!popUpManager) {
            popUpManager = FindObjectOfType<PopUpManager>();
        }
        popUpManager.enabled = true;

        if (lastWidth != Screen.width || lastHeight != Screen.height)
        {
            if (lastTutorialTrigger != null && popUpManager.IsShowing())
            {
                StartCoroutine(RecalculateShownTutorial());
            }

            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }

        if (popUpManager.IsShowing()) return;

        TutorialTrigger prior = TryPopPriorityTriggers();
        TutorialTrigger cond = TryPopConditionalTriggers();

        if (prior != null && cond != null)
        {
            if (prior.CompareTo(cond) <= 0)
            {
                ShowTutorialInfo(prior);
                AddTutorialTrigger(cond);
                return;
            }

            ShowTutorialInfo(cond);
            AddTutorialTrigger(prior);
        }
        else if (prior != null)
        {
            ShowTutorialInfo(prior);
        }
        else if (cond != null)
        {
            ShowTutorialInfo(cond);
        }
    }

    private TutorialTrigger TryPopPriorityTriggers() {
        if (priorTriggers.Count == 0) return null;
        if (popUpManager == null) return null;
        if (popUpManager.IsShowing()) return null;

        return priorTriggers.Remove();
    }

    private TutorialTrigger TryPopConditionalTriggers() {
        if (conditionTriggers.Count == 0) return null;
        if (popUpManager == null) return null;
        if (popUpManager.IsShowing()) return null;

        TutorialTrigger trigger = conditionTriggers[0];
        if (trigger.condition.Invoke()) return trigger;

        return null;
    }

    private void ShowTutorialInfo(TutorialTrigger t) {
        if (t == null) return;

        if(lastTutorialTrigger != null) {
            if (lastTutorialTrigger.destroyOnShowed)
                Destroy(lastTutorialTrigger);
            lastTutorialTrigger = null;
        }

        PopUpData info = t.info;

        if (t.highlightObject)
            popUpManager.Show(info, t.GetRect());
        else
            popUpManager.Show(info);

        if (TemaryManager.Instance != null)
            TemaryManager.Instance.AddTemary(t.info);

        if (t.OnShowed != null)
            t.OnShowed.Invoke();

        if (!triggered.Contains(t.GetHash()))
            triggered.Add(t.GetHash());

        if (!saved.Contains(t.GetHash()))
            savePending.Add(t);

        if (t.isSaveCheckpoint)
            SavePendingTriggers();

        lastTutorialTrigger = t;

        /*if (t.destroyOnShowed)
            Destroy(t);*/

        // Save the advance
        SaveManager.Instance.Save();
    }

    public void AddTutorialTrigger(TutorialTrigger t, bool checkTriggered = false) {
        if (t && checkTriggered && triggered.Contains(t.GetHash())) return;

        if (t.condition != null) {
            if (conditionTriggers.Find((TutorialTrigger other) => other.GetHash() == t.GetHash()) != null) 
                return;

            conditionTriggers.Add(t);
            conditionTriggers.Sort();
        }
        else {
            List<TutorialTrigger> prior = priorTriggers.ToList();
            if (prior.Find((TutorialTrigger other) => other.GetHash() == t.GetHash()) != null) 
                return;
            priorTriggers.Add(t);
        }
    }

    public HashSet<string> GetTriggeredTutorials() {
        return triggered;
    }

    public void Load(TutorialSaveData data) {
        for (int i = 0; i < data.tutorials.Length; i++)
        {
            saved.Add(data.tutorials[i]);
            triggered.Add(data.tutorials[i]);
        }
    }

    public TutorialSaveData Save() {
        string[] array = new string[saved.Count];
        saved.CopyTo(array);
        TutorialSaveData data = new TutorialSaveData();
        data.tutorials = array;
        return data;
    }

    private void SavePendingTriggers()
    {
        foreach (TutorialTrigger trigger in savePending)
        {
            string hash = trigger.GetHash();
            saved.Add(hash);
        }
        Save();

        savePending.Clear();
    }

    private IEnumerator RecalculateShownTutorial() {
        yield return new WaitForEndOfFrame();

        if (lastTutorialTrigger.highlightObject)
            popUpManager.Show(lastTutorialTrigger.info, lastTutorialTrigger.GetRect());
        else
            popUpManager.Show(lastTutorialTrigger.info);
    }
}
