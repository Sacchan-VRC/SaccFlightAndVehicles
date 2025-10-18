
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Tractor : UdonSharpBehaviour
    {
        [Header("The orientation of these two is important. Green should be pointing 'up' for a ufo (direction of the gun)")]
        public Transform TractorTop;
        public Transform TractorCenter;
        [Header("Players will launch towards the blue arrow of LaunchDirection")]
        public Transform LaunchDirection;
        public float tractorHeight = 100f;
        public float tractorRadius = 15f;
        public float PullRadius = 3;
        public float PullRadius_Min = 1;
        public bool JumpToEscape = true;
        public bool RespawnToEscape = true;
        public GameObject TractorFX;
        public float LaunchStrength;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;
        public Transform AtGCam;
        public GameObject AtGScreen;
        [SerializeField] private LayerMask TractorRaycastLayers = 2065;
        private float TractorDisableTime;
        private bool InVR;
        private bool InTractor;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [SerializeField] Animator BoolAnimator;
        private bool DoAnimBoolWhenOn;
        private bool HoldingTrigger = false;
        [SerializeField] private string AnimBool = string.Empty;
        [UdonSynced, FieldChangeCallback(nameof(TractorOn))] public bool _TractorOn;
        public bool TractorOn
        {
            set
            {
                if (Dial_Funcon) { Dial_Funcon.SetActive(value); }
                for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(value); }
                if (!InVR)
                {
                    if (AtGCam) { AtGCam.gameObject.SetActive(value); }
                    if (AtGScreen) { AtGScreen.SetActive(value); }
                }
                if (!value)
                {
                    if (InTractor && LaunchDirection)
                    {
                        Networking.LocalPlayer.SetVelocity(LaunchDirection.forward * LaunchStrength);
                    }
                    InTractor = false;
                }
                if (DoAnimBoolWhenOn) { BoolAnimator.SetBool(AnimBool, value); }
                TractorFX.gameObject.SetActive(value);
                _TractorOn = value;
            }
            get => _TractorOn;
        }
        public void SFEXT_L_EntityStart() { Init(); }
        public void SFEXT_O_PilotEnter() { UserEnter(); }
        public void SFEXT_O_PilotExit() { UserExit(); }
        public void SFEXT_O_OnPickup() { UserEnter(); }
        public void SFEXT_O_OnDrop() { UserExit(); }
        private void Init()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(false); }
            InVR = EntityControl.InVR;
            if (AnimBool != string.Empty) { DoAnimBoolWhenOn = true; }
        }
        private void UserEnter()
        {
            TractorOn = false;
        }
        private void UserExit()
        {
            HoldingTrigger = false;
            RequestSerialization();
            if (!InVR)
            {
                if (AtGCam) { AtGCam.gameObject.SetActive(false); }
                if (AtGScreen) { AtGScreen.SetActive(false); }
            }
        }
        public void SFEXT_G_OnDrop()
        {
            SFEXT_G_PilotExit();
        }
        public void SFEXT_G_OnPickup()
        {
            SFEXT_G_PilotEnter();
        }
        byte numUsers;
        public void SFEXT_G_PilotEnter()
        {
            numUsers++;
            if (numUsers > 1) return;

            EnableForOthers();
        }
        public void SFEXT_G_PilotExit()
        {
            numUsers--;
            if (numUsers != 0) return;

            DisableForOthers();
        }
        public void EnableForOthers()
        {
            InTractor = false;
            gameObject.SetActive(true);
        }
        public void DisableForOthers()
        {
            InTractor = false;
            gameObject.SetActive(false);
            TractorOn = false;
        }
        void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75 || HoldingTrigger)
                {
                    if (!TriggerLastFrame)
                    {//new button press
                        TractorOn = !TractorOn;
                        RequestSerialization();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            if (TractorOn)
            {
                Vector3 myPos = Networking.LocalPlayer.GetPosition();
                float Dist = Vector3.Distance(myPos, TractorCenter.position);
                // dbgDist = Dist;
                Vector3 SuckPoint;
                RaycastHit TractorHit;
                if (Physics.Raycast(TractorTop.position, TractorCenter.position - TractorTop.position, out TractorHit, Vector3.Distance(TractorTop.position, TractorCenter.position), TractorRaycastLayers, QueryTriggerInteraction.Ignore))
                { SuckPoint = TractorHit.point; }
                else
                { SuckPoint = TractorCenter.position; }
                if (InTractor) { SuckPlayer(Dist, SuckPoint); return; }
                if (Dist < tractorHeight && Time.time - TractorDisableTime > 5f)
                {
                    Vector3 myTractorCoords = TractorCenter.InverseTransformDirection(transform.position - myPos);
                    // dbgmyTractorCoords = myTractorCoords;
                    if (Mathf.Abs(myTractorCoords.x) < tractorRadius)
                    {
                        if (Mathf.Abs(myTractorCoords.z) < tractorRadius)
                        {
                            InTractor = true;
                            SuckPlayer(Dist, SuckPoint);
                        }
                    }
                }
            }
        }
        public override void InputJump(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            if (InTractor && args.boolValue)
            {
                if (JumpToEscape)
                {
                    TractorDisableTime = Time.time;
                    InTractor = false;
                }
            }
        }
        private void SuckPlayer(float Dist, Vector3 SuckPoint)
        {
            if (Dist > PullRadius)
            {
                Networking.LocalPlayer.TeleportTo(Vector3.Lerp(Networking.LocalPlayer.GetPosition(), SuckPoint, 1 - Mathf.Pow(0.5f, 4 * Time.deltaTime)), Networking.LocalPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, true);
            }
            else if (Dist > PullRadius_Min)
            {
                Networking.LocalPlayer.TeleportTo(Vector3.Lerp(Networking.LocalPlayer.GetPosition(), SuckPoint, 1 - Mathf.Pow(0.5f, 0.4f * Time.deltaTime)), Networking.LocalPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, true);
            }
            Networking.LocalPlayer.SetVelocity(Vector3.zero);
        }
        private bool Selected;
        public void DFUNC_Selected()
        {
            Selected = true;
            if (AtGCam)
            {
                if (AtGCam) { AtGCam.gameObject.SetActive(true); }
                if (AtGScreen) { AtGScreen.SetActive(true); }
            }
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
            if (AtGScreen) { AtGScreen.SetActive(false); }
            HoldingTrigger = false;
        }
        public void KeyboardInput()
        {
            TractorOn = !TractorOn;
            RequestSerialization();
        }
        public void SFEXT_O_OnPickupUseDown()
        {
            HoldingTrigger = Selected;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            HoldingTrigger = false;
        }
        private bool TriggerLastFrame;
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                if (RespawnToEscape)
                {
                    TractorDisableTime = Time.time; InTractor = false;
                }
            }
        }
#if UNITY_EDITOR
        [Header("Editor Only")]
        public bool DrawDebugGizmos;
        private void OnDrawGizmosSelected()
        {
            if (DrawDebugGizmos)
            {
                Gizmos.DrawWireCube(TractorCenter.position, Vector3.one * tractorRadius);
                Gizmos.DrawWireCube(TractorTop.position, Vector3.one * tractorRadius);
                Gizmos.DrawWireSphere(TractorCenter.position, tractorHeight);
            }
        }
#endif
    }
}