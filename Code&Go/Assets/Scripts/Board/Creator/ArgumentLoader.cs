using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xasu.HighLevel;

public class ArgumentLoader : MonoBehaviour
{
    [SerializeField] private ArgInput[] inputs;
    [SerializeField] private Text text;

    private BoardObject currentObject;

    public void SetBoardObject(BoardObject newObject)
    {
        if (newObject == null) return;

        currentObject = newObject;
        gameObject.SetActive(true);
        text.text = currentObject.GetNameWithIndex();

        string[] argsNames = currentObject.GetArgsNames();
        if (argsNames.Length == 0) gameObject.SetActive(false);
        for (int i = 0; i < inputs.Length; i++)
        {
            if (i < argsNames.Length)
            {
                int index = i;
                inputs[i].Init((bool action) => TraceCheckBox(action, argsNames[index]));
                inputs[i].FillArg(argsNames[i]);
            }
            else
                inputs[i].gameObject.SetActive(false);
        }
    }

    private void TraceCheckBox(bool active, string argName)
    {
        GameObjectTracker.Instance.Interacted("argument_checkbox")
            .WithResultExtension("articoding://ext/argument_name", argName.ToLower())
            .WithResultExtension("articoding://ext/element_type", currentObject.GetName().ToLower())
            .WithResultExtension("articoding://ext/element_name", currentObject.GetNameWithIndex().ToLower())
            .WithResultExtension("articoding://ext/element_id", currentObject.GetID().ToLower())
            .WithResultExtension("articoding://ext/old_value", !active)
            .WithResultExtension("articoding://ext/new_value", active)
            .WithResultExtension("articoding://ext/action", "change_value");
    }

    public void LoadArgs()
    {
        if (currentObject == null) return;

        string[] args = new string[inputs.Length];
        var promise = GameObjectTracker.Instance.Interacted(currentObject.GetID().ToLower())
            .WithResultExtension("articoding://ext/element_type", currentObject.GetName().ToLower())
            .WithResultExtension("articoding://ext/element_name", currentObject.GetNameWithIndex().ToLower())
            .WithResultExtension("articoding://ext/action", "state_change");

        for (int i = 0; i < args.Length; i++)
        {
            if (inputs[i].gameObject.activeSelf)
            {
                args[i] = inputs[i].GetInput();
                promise
                    .WithResultExtension("articoding://ext/arg_name", currentObject.GetArgsNames()[i].ToLower())
                    .WithResultExtension("articoding://ext/arg_value", args[i].ToLower());
            }
        }

        currentObject.LoadArgs(args);
    }

    private void Update()
    {
        if (currentObject == null && gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
