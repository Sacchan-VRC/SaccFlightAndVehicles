
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccViewScreenController : UdonSharpBehaviour
    {
        [Tooltip("If enabled, disable when player is this many meters away from this gameobject")]
        public float DisableDistance = 15;
        [Tooltip("Camera that follows the planes")]
        public Camera Cam;
        [Tooltip("Screen that is enabled when turned on")]
        public GameObject ViewScreen;
        public GameObject OnButton;
        public GameObject SyncToggle;
        public bool DisableSwitchToDead;
        [Header("Campositions are filled on build, check it's tooltip")]
        [Tooltip("Disable the auto fill?")]
        public bool CamPosAutoFill = true;
        public string CamposSuffix = ":campos";
        [Tooltip("Click Find Campositions in Scene to test if your cameraposition is added, this function is run on build.\nName an object in the scene :campos<name> in order for it to get added to this list\n add FOV:60 to the end of the name to set FOV\nExample name: :camposVehicleCamFOV:20")]
        public Transform[] CamPositions;
        public Text ChannelNumberText;
        public bool ShowChannelNumber = true;
        private float StartFov;
        [System.NonSerializedAttribute] public GameObject[] CamTargets = new GameObject[0];
        public int CurrentTarget_Local;
        bool syncedChannel = true;
        [UdonSynced, FieldChangeCallback(nameof(CurrentTarget)), System.NonSerializedAttribute] public int _CurrentTarget;
        public int CurrentTarget
        {
            set
            {
                if (syncedChannel)
                { CurrentTarget_Local = value; }
                _CurrentTarget = value;
                if (!Disabled)
                {
                    UpdateCamera();
                }
            }
            get => _CurrentTarget;
        }
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public bool Disabled = true;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public Transform CameraTransform;
        [System.NonSerializedAttribute] public SaccEntity TargetEntity;
        [System.NonSerializedAttribute] public Transform TargetCoM;
        private bool Initialized = false;
        private void Start()
        {
            Init();
        }
        public void Init()
        {
            if (Initialized) { return; }
            Initialized = true;
            CameraTransform = Cam.transform;
            StartFov = Cam.fieldOfView;
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null) { InEditor = false; }

            CamTargets = new GameObject[CamPositions.Length];
            for (int i = 0; i < CamPositions.Length; i++)
            {
                SaccEntity TargetEntityStart = null;
                if (CamPositions[i])
                {
                    GameObject TargObjs = CamPositions[i].gameObject;
                    while (!TargetEntityStart && TargObjs.transform.parent)
                    {
                        TargObjs = TargObjs.transform.parent.gameObject;
                        TargetEntityStart = TargObjs.GetComponent<SaccEntity>();
                    }
                }

                if (TargetEntityStart)
                {
                    CamTargets[i] = TargetEntityStart.gameObject;
                }
                else
                {
                    CamTargets[i] = null;
                }
            }

            Disabled = false;
            TurnOff();
        }
        public void ActiveUpdate()
        {
            if (!Disabled)
            {
                //disable if far away
                if (!InEditor)
                {
                    if (Vector3.Distance(localPlayer.GetPosition(), transform.position) > DisableDistance)
                    {
                        TurnOff();
                        return;
                    }
                }
                UpdateChannelText();
                SendCustomEventDelayedSeconds(nameof(ActiveUpdate), .3f);
            }
        }
        private void UpdateChannelText()
        {
            if (TargetEntity)
            {
                ChannelNumberText.text = (ShowChannelNumber ? (CurrentTarget_Local + 1).ToString() + "\n" : "") + TargetEntity.UsersName;
            }
            else
            {
                if (ShowChannelNumber)
                { ChannelNumberText.text = string.Concat((CurrentTarget_Local + 1).ToString()); }
            }
        }
        public void TurnOff()
        {
            if (Disabled) { return; }
            if (OnButton) OnButton.SetActive(true);
            if (SyncToggle) SyncToggle.SetActive(false);
            Disabled = true;
            ViewScreen.SetActive(false);
            Cam.gameObject.SetActive(false);
            ChannelNumberText.text = string.Empty;
        }
        public void TurnOn()
        {
            if (!Disabled) { return; }
            if (OnButton) OnButton.SetActive(false);
            if (SyncToggle) SyncToggle.SetActive(true);
            Disabled = false;
            Cam.gameObject.SetActive(true);
            ViewScreen.SetActive(true);
            CameraTransform.parent = CamPositions[CurrentTarget_Local];
            CameraTransform.localPosition = Vector3.zero;
            CameraTransform.localRotation = Quaternion.identity;
            UpdateCamera();
            SendCustomEvent(nameof(ActiveUpdate));
        }
        public void ChannelUp()
        {
            int NumCameras = CamPositions.Length;
            int nextcam = CurrentTarget_Local + 1;
            if (nextcam >= NumCameras) { nextcam = 0; }
            int numchecks = 0;

            SaccEntity nextTargEntity;
            if (CamTargets[nextcam])
            { nextTargEntity = CamTargets[nextcam].GetComponent<SaccEntity>(); }
            else { nextTargEntity = null; }
            while (!CamPositions[nextcam] || !CamPositions[nextcam].gameObject.activeInHierarchy || (DisableSwitchToDead && (!nextTargEntity || nextTargEntity.dead)))
            {
                if (numchecks >= NumCameras)
                {
                    // Debug.LogWarning("ViewScreen: No valid cameras");
                    return;
                }
                if (CamTargets[nextcam])
                { nextTargEntity = CamTargets[nextcam].GetComponent<SaccEntity>(); }
                else
                { nextTargEntity = null; }
                nextcam++;
                if (nextcam >= NumCameras) { nextcam = 0; }
                numchecks++;
            }
            if (syncedChannel)
            {
                if (!Networking.LocalPlayer.IsOwner(gameObject))
                { Networking.SetOwner(localPlayer, gameObject); }
                CurrentTarget = nextcam;
                RequestSerialization();
            }
            else
            {
                CurrentTarget_Local = nextcam;
                UpdateCamera();
            }
        }
        public void ChannelDown()
        {
            int NumCameras = CamPositions.Length;
            int nextcam = CurrentTarget_Local - 1;
            if (nextcam < 0) { nextcam = NumCameras - 1; }
            int numchecks = 0;

            SaccEntity nextTargEntity;
            if (CamTargets[nextcam])
            { nextTargEntity = CamTargets[nextcam].GetComponent<SaccEntity>(); }
            else { nextTargEntity = null; }
            while (!CamPositions[nextcam] || !CamPositions[nextcam].gameObject.activeInHierarchy || (DisableSwitchToDead && (!nextTargEntity || nextTargEntity.dead == true)))
            {
                if (numchecks >= NumCameras)
                {
                    // Debug.LogWarning("ViewScreen: No valid cameras");
                    return;
                }
                if (CamTargets[nextcam])
                { nextTargEntity = CamTargets[nextcam].GetComponent<SaccEntity>(); }
                else
                { nextTargEntity = null; }
                nextcam--;
                if (nextcam < 0) { nextcam = NumCameras - 1; }
                numchecks++;
            }
            if (syncedChannel)
            {
                if (!Networking.LocalPlayer.IsOwner(gameObject))
                { Networking.SetOwner(localPlayer, gameObject); }
                CurrentTarget = nextcam;
                RequestSerialization();
            }
            else
            {
                CurrentTarget_Local = nextcam;
                UpdateCamera();
            }
        }
        public void ToggleSyncedChannel()
        {
            syncedChannel = !syncedChannel;
            if (syncedChannel)
            {
                CurrentTarget_Local = _CurrentTarget;
            }
            UpdateCamera();
        }
        public void UpdateCamera()
        {
            if (CamPositions[CurrentTarget_Local])
            {
                CameraTransform.parent = CamPositions[CurrentTarget_Local];
                string camname = CamPositions[CurrentTarget_Local].name;
                if (camname.Contains("FOV:"))
                {
                    string[] FovSplit = camname.Split(':');
                    float newfov;
                    if (float.TryParse(FovSplit[FovSplit.Length - 1], out newfov))
                    {
                        Cam.fieldOfView = float.Parse(FovSplit[FovSplit.Length - 1]);
                    }
                    else
                    {
                        Cam.fieldOfView = StartFov;
                    }
                }
                else
                {
                    Cam.fieldOfView = StartFov;
                }
            }
            CameraTransform.localPosition = Vector3.zero;
            CameraTransform.localRotation = Quaternion.identity;
            if (CamTargets[CurrentTarget_Local])
            {
                TargetEntity = CamTargets[CurrentTarget_Local].GetComponent<SaccEntity>();
            }
            else
            {
                TargetEntity = null;
            }
            UpdateChannelText();
        }
    }
}