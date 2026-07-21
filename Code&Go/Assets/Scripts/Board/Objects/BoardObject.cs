using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

// Base class of all board objects
public class BoardObject : MonoBehaviour
{
    public enum Direction { RIGHT, DOWN_RIGHT, DOWN, DOWN_LEFT, LEFT, UP_LEFT, UP, UP_RIGHT, PARTIAL };

    static Dictionary<string, int> IDs = new Dictionary<string, int>();
    static int numIDs = 0;

    Direction objectDirection;

    [SerializeField] protected Animator anim;
    protected BoardManager boardManager;
    protected int index = 0;

    [SerializeField] protected string typeName = "";
    [SerializeField] protected string[] argsNames;

    [SerializeField] protected LocalizedString typeNameLocalized;
    [SerializeField] protected LocalizedString[] argsNamesLocalized;

    [SerializeField] protected bool isMovable;
    [SerializeField] protected bool isRotatable;

    private string id;

    private static string GenerateRandomBase58Key(int length)
    {
        var alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, length).Select(_ => alphabet[UnityEngine.Random.Range(0, alphabet.Length)]).ToArray()).ToLower();
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(id))
            id = GetNameAsLower() + "_" + GenerateRandomBase58Key(4);
    }

    public virtual void SetBoard(BoardManager board)
    {
        boardManager = board;
    }

    public int GetObjectID()
    {
        if (!IDs.ContainsKey(this.GetType().Name))
            IDs[this.GetType().Name] = numIDs++;
        return IDs[this.GetType().Name];
    }

    public string GetName()
    {
        if (typeNameLocalized.IsEmpty || !typeNameLocalized.GetLocalizedStringAsync().IsDone)
            return typeName;

        string res = typeNameLocalized.GetLocalizedStringAsync().Result;
        return res;
        //return typeName;
    }

    public string GetID()
    {
        if (string.IsNullOrEmpty(id))
            Awake();
        return id;
    }

    public string GetNameAsLower()
    {
        if (typeNameLocalized.IsEmpty || !typeNameLocalized.GetLocalizedStringAsync().IsDone)
            return typeName.ToLower();

        string res = typeNameLocalized.GetLocalizedStringAsync().Result;
        return res.ToLower();
        //return typeName.ToLower();
    }

    public string GetNameWithIndex()
    {
        if (typeNameLocalized.IsEmpty || !typeNameLocalized.GetLocalizedStringAsync().IsDone)
            return typeName + index.ToString();

        string res = typeNameLocalized.GetLocalizedStringAsync().Result;
        return res + "_" + index.ToString();
        //return typeName + index.ToString();
    }

    public string[] GetArgsNames()
    {

        string[] argsArr = new string[argsNamesLocalized.Length];

        for (int i = 0; i < argsNamesLocalized.Length; i++)
        {
            if (!argsNamesLocalized[i].GetLocalizedStringAsync().IsDone)
                return argsNames;
            argsArr[i] = argsNamesLocalized[i].GetLocalizedStringAsync().Result;
        }
        return argsArr;

        //return argsNames;
    }

    public void SetDirection(Direction direction, bool rotate = true)
    {
        objectDirection = direction;

        if (!rotate) return;

        if (direction == Direction.PARTIAL)
            direction = Direction.RIGHT; // Default

        Vector3 lastRot = transform.localEulerAngles;
        lastRot.y = (int)direction * 45.0f;
        transform.localEulerAngles = lastRot;
    }

    public Direction GetDirection()
    {
        return objectDirection;
    }

    public void Rotate(int dir)
    {
        Direction newDir = (Direction)((((int)objectDirection + dir) + 8) % 8);
        SetDirection(newDir, isRotatable);
    }

    public Animator GetAnimator()
    {
        return anim;
    }

    public void SetIndex(int index)
    {
        this.index = index;
    }

    public int GetIndex()
    {
        return index;
    }

    public bool IsMovable()
    {
        return isMovable;
    }

    public bool IsRotatable()
    {
        return isRotatable;
    }

    public void SetMovable(bool movable)
    {
        isMovable = movable;
    }
    public void SetRotatable(bool rotatable)
    {
        isRotatable = rotatable;
    }


    //Virtual method for seralization of additional arguments in child classes
    public virtual string[] GetArgs() { return new string[] { }; }

    //Virtual method for de-seralization of additional arguments in child classes
    public virtual void LoadArgs(string[] args) { }
}
