
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccMultiObjectToggle : UdonSharpBehaviour
{
    public GameObject[] ToggleObjs;
    [Tooltip("Not required, script to disable objects if player goes too far away")]
    public GameObject Disabler;
    public bool CanToggleToDisabled = false;
    public bool DisabledDefault = false;
    [System.NonSerializedAttribute, FieldChangeCallback(nameof(current))] public int _current = -1;
    public int current
    {
        set
        {
            if (value < 0 || value >= ToggleObjs.Length)//set this from another script if you want to disable all
            {
                if (Disabler) { Disabler.SetActive(false); }
            }
            else
            {
                if (Disabler) { Disabler.SetActive(true); }
            }

            for (int i = 0; i < ToggleObjs.Length; i++)
            {
                ToggleObjs[i].SetActive(i == value);
            }
            _current = value;
        }
        get => _current;
    }
    private void Start()
    {
        if (DisabledDefault)
        { current = -1; }
        else
        { current = 0; }
    }
    public override void Interact()
    {
        Switch();
    }
    public void Switch()
    {
        if (current + 1 >= ToggleObjs.Length)
        {
            if (CanToggleToDisabled)
            { current = -1; }
            else
            { current = 0; }
        }
        else
        { current++; }
    }
}
