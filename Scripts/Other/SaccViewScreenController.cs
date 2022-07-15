
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SaccFlightAndVehicles
{
    [CustomEditor(typeof(SaccViewScreenController))]
    public class SaccViewScreenControllerEditor : Editor
    {
        const string TransformPrefix = ":campos";
        public override void OnInspectorGUI()
        {
            SaccViewScreenController _target = target as SaccViewScreenController;
            // We make sure that Udon Behaviour header is drawn
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawDefaultInspector();
            if (GUILayout.Button("Find Campositions In Scene"))
            {
                Undo.RecordObject(target, "Array objects changed");
                var AllCamsArray = GetAllSceneCameraPositions().ToArray();
                PutCamPositionsInArray(_target, AllCamsArray);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                EditorUtility.SetDirty(_target);//needed
            }
        }
        public void PutCamPositionsInArray(SaccViewScreenController input, Transform[] pos)
        {
            input.CamPositions = pos;
        }
        List<Transform> GetAllSceneCameraPositions(Transform tr = null, List<Transform> ls = null)
        {
            if (tr == null)
            {
                ls = ls ?? new List<Transform>();
                foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) GetAllSceneCameraPositions(g.transform, ls);
            }
            else
            {
                if (tr.name.StartsWith(TransformPrefix, System.StringComparison.InvariantCultureIgnoreCase)) ls.Add(tr);
                foreach (Transform t in tr) GetAllSceneCameraPositions(t, ls);
            }
            return ls;
        }
    }
}
#endif

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
        [Header("Campositions is overriden on build, check it's tooltip")]
        [Tooltip("Click Find Campositions in Scene to test if your cameraposition is added, this function is run on build.\nName an object in the scene :campos<name> in order for it to get added to this list\n add FOV:60 to the end of the name to set FOV\nExample name: :camposVehicleCamFOV:20")]
        public Transform[] CamPositions;
        public Text ChannelNumberText;
        private float StartFov;
        [System.NonSerializedAttribute] public GameObject[] CamTargets = new GameObject[80];
        [UdonSynced, FieldChangeCallback(nameof(CurrentTarget))] private int _CurrentTarget;
        public int CurrentTarget
        {
            set
            {
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
        private Transform CameraTransform;
        private SaccEntity TargetEntity;
        private Transform TargetCoM;
        void Start()
        {
            CameraTransform = Cam.transform;
            StartFov = Cam.fieldOfView;
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null) { InEditor = false; }
            //get array of AAM Targets

            //populate AAMTargets list
            int n = 0;
            foreach (Transform camtarget in CamPositions)
            {
                SaccEntity TargetEntityStart = null;
                if (camtarget)
                {
                    GameObject TargObjs = camtarget.gameObject;
                    while (!TargetEntityStart && TargObjs.transform.parent)
                    {
                        TargObjs = TargObjs.transform.parent.gameObject;
                        TargetEntityStart = TargObjs.GetComponent<SaccEntity>();
                    }
                }

                if (TargetEntityStart)
                {
                    CamTargets[n] = TargetEntityStart.gameObject;
                }
                else
                {
                    CamTargets[n] = null;
                }
                n++;
            }
            n = 0;

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
                ChannelNumberText.text = string.Concat((CurrentTarget + 1).ToString(), "\n", TargetEntity.UsersName);
            }
            else
            {
                ChannelNumberText.text = string.Concat((CurrentTarget + 1).ToString());
            }
        }
        public void TurnOff()
        {
            if (Disabled) { return; }
            Disabled = true;
            ViewScreen.SetActive(false);
            Cam.gameObject.SetActive(false);
            ChannelNumberText.text = string.Empty;
        }
        public void TurnOn()
        {
            if (!Disabled) { return; }
            Disabled = false;
            Cam.gameObject.SetActive(true);
            ViewScreen.SetActive(true);
            CameraTransform.parent = CamPositions[CurrentTarget];
            CameraTransform.localPosition = Vector3.zero;
            CameraTransform.localRotation = Quaternion.identity;
            UpdateCamera();
            SendCustomEvent(nameof(ActiveUpdate));
        }
        public void ChannelUp()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            int NumCameras = CamPositions.Length;
            int nextcam = CurrentTarget + 1;
            if (nextcam >= NumCameras) { nextcam = 0; }
            int numchecks = 0;
            while (!CamPositions[nextcam] || !CamPositions[nextcam].gameObject.activeInHierarchy)
            {
                if (numchecks >= NumCameras)
                {
                    Debug.LogWarning("ViewScreen: No valid cameras");
                    return;
                }
                nextcam++;
                if (nextcam >= NumCameras) { nextcam = 0; }
            }
            CurrentTarget = nextcam;
            RequestSerialization();
        }
        public void ChannelDown()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }
            int NumCameras = CamPositions.Length;
            int nextcam = CurrentTarget - 1;
            if (nextcam < 0) { nextcam = NumCameras - 1; }
            int numchecks = 0;
            while (!CamPositions[nextcam] || !CamPositions[nextcam].gameObject.activeInHierarchy)
            {
                if (numchecks >= NumCameras)
                {
                    Debug.LogWarning("ViewScreen: No valid cameras");
                    return;
                }
                nextcam--;
                if (nextcam < 0) { nextcam = NumCameras - 1; }
            }
            CurrentTarget = nextcam;
            RequestSerialization();
        }
        public void UpdateCamera()
        {
            if (CamPositions[CurrentTarget])
            {
                CameraTransform.parent = CamPositions[CurrentTarget];
                string camname = CamPositions[CurrentTarget].name;
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
            if (CamTargets[CurrentTarget])
            {
                TargetEntity = CamTargets[CurrentTarget].GetComponent<SaccEntity>();
            }
            else
            {
                TargetEntity = null;
            }
            UpdateChannelText();
        }
    }
}