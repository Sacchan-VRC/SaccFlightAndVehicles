
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccMultiObjectToggle : UdonSharpBehaviour
    {
        public GameObject[] ToggleObjs;
        [Tooltip("Another object that is enabled unless everything is disabled")]
        public GameObject EnabledWithAll;
        public bool ResetIfPlayerDistant;
        [Tooltip("How distant?")]
        public float DisableDistance = 15;
        private bool CheckActive;
        public bool CanToggleToDisabled = false;
        [Tooltip("0 = first page, -1 = disabled")]
        public int DefaultPage = 0;
        [Tooltip("Overrides Default Page")]
        public bool StartOnRandomPage = false;
        [Tooltip("CB_PageChange")]
        public UdonSharpBehaviour[] PageChangeCallbacks;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(current))] public int _current = -1;
        public int current
        {
            set
            {
                _current = value;
                if (value < 0 || value >= ToggleObjs.Length)//set this from another script if you want to disable all
                {
                    if (EnabledWithAll) { EnabledWithAll.SetActive(false); }
                }
                else
                {
                    if (EnabledWithAll) { EnabledWithAll.SetActive(true); }
                    if (ResetIfPlayerDistant)
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
                for (int i = 0; i < PageChangeCallbacks.Length; i++)
                {
                    PageChangeCallbacks[i].SendCustomEvent("CB_PageChange");
                }
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
                    current = DefaultPage;
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
            if (StartOnRandomPage)
            { current = Random.Range(0, ToggleObjs.Length); }
            else
            { current = DefaultPage; }
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
        public void SwitchBack()
        {
            if (CanToggleToDisabled)
            {
                if (current - 1 <= -2)
                {
                    current = ToggleObjs.Length - 1;
                }
                else
                { current--; }
            }
            else if (current - 1 <= -1)
            {
                current = ToggleObjs.Length - 1;
            }
            else
            { current--; }
        }
    }
}