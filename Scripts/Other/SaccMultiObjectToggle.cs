
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccMultiObjectToggle : UdonSharpBehaviour
{
    public GameObject[] ToggleObjs;
    [Tooltip("Another object that is enabled unless everything is disabled")]
    public GameObject EnabledWithAll;
    public bool DisableIfPlayerDistant;
    [Tooltip("How distant?")]
    public float DisableDistance = 15;
    private bool CheckActive;
    public bool CanToggleToDisabled = false;
    public bool DisabledDefault = false;
    [System.NonSerializedAttribute, FieldChangeCallback(nameof(current))] public int _current = -1;
    public int current
    {
        set
        {
            if (value < 0 || value >= ToggleObjs.Length)//set this from another script if you want to disable all
            {
                if (EnabledWithAll) { EnabledWithAll.SetActive(false); }
            }
            else
            {
                if (EnabledWithAll) { EnabledWithAll.SetActive(true); }
                if (DisableIfPlayerDistant)
                {
                    if (!CheckActive)
                    {
                        CheckActive = true;
                        CheckDisable();
                    }
                }
            }

            for (int i = 0; i < ToggleObjs.Length; i++)
            {
                ToggleObjs[i].SetActive(i == value);
            }
            _current = value;
        }
        get => _current;
    }
    private VRCPlayerApi localPlayer;
    public void CheckDisable()
    {
        if (CheckActive && localPlayer != null)
        {
            if (Vector3.Distance(localPlayer.GetPosition(), gameObject.transform.position) > DisableDistance)
            {
                current = -1;
                CheckActive = false;
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(CheckDisable), 1);
            }
        }
    }
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
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
