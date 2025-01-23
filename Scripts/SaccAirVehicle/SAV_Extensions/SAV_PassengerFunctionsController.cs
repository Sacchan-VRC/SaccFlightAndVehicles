
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_PassengerFunctionsController : UdonSharpBehaviour
    {
        [Tooltip("Put all scripts used by this vehicle that use the event system into this list (excluding DFUNCs)")]
        public UdonSharpBehaviour[] PassengerExtensions;
        [Tooltip("Function dial scripts that you wish to be on the left dial")]
        public UdonSharpBehaviour[] Dial_Functions_L;
        [Tooltip("Function dial scripts that you wish to be on the right dial")]
        public UdonSharpBehaviour[] Dial_Functions_R;
        [Tooltip("Should there be a function at the top middle of the function dial[ ]? Or a divider[x]? Useful for adjusting function positions with an odd number of functions")]
        public bool LeftDialDivideStraightUp = false;
        [Tooltip("See above")]
        public bool RightDialDivideStraightUp = false;
        [Tooltip("Object that points toward the currently selected function on the left stick")]
        public Transform LStickDisplayHighlighter;
        [Tooltip("Object that points toward the currently selected function on the right stick")]
        public Transform RStickDisplayHighlighter;
        [Header("Selection Sound")]

        [Tooltip("Oneshot sound played when switching functions")]
        public AudioSource SwitchFunctionSound;
        public bool PlaySelectSoundLeft = true;
        public bool PlaySelectSoundRight = true;
        public float DialSensitivity = 0.7f;
        private int LStickNumFuncs;
        private int RStickNumFuncs;
        private float LStickFuncDegrees;
        private float RStickFuncDegrees;
        [System.NonSerializedAttribute] public float LStickFuncDegreesDivider;
        [System.NonSerializedAttribute] public float RStickFuncDegreesDivider;
        private bool[] LStickNULL;
        private bool[] RStickNULL;
        private Vector2 LStickCheckAngle;
        private Vector2 RStickCheckAngle;
        private VRCPlayerApi localPlayer;
        private bool InEditor = true;
        private bool InVR = false;
        [System.NonSerializedAttribute] public int RStickSelection = -1;
        [System.NonSerializedAttribute] public int LStickSelection = -1;
        [System.NonSerializedAttribute] public int RStickSelectionLastFrame = -1;
        [System.NonSerializedAttribute] public int LStickSelectionLastFrame = -1;
        public bool IsOwner = false;
        public bool FunctionsActive = false;
        public bool Occupied = false;
        VRCPlayerApi currentUser;
        [NonSerialized] public VRCStation Station;
        private bool DoDialLeft;
        private bool DoDialRight;
        [NonSerialized] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public bool _DisableLeftDial;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableLeftDial_))] public int DisableLeftDial = 0;
        public int DisableLeftDial_
        {
            set { _DisableLeftDial = value > 0; }
            get => DisableLeftDial;
        }
        [System.NonSerializedAttribute] public bool _DisableRightDial;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableRightDial_))] public int DisableRightDial = 0;
        public int DisableRightDial_
        {
            set { _DisableRightDial = value > 0; }
            get => DisableRightDial;
        }
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            IsOwner = EntityControl.IsOwner;
            if (localPlayer != null)
            {
                InEditor = false;
                InVR = EntityControl.InVR;
            }

            //Dial Stuff
            LStickNumFuncs = Dial_Functions_L.Length;
            RStickNumFuncs = Dial_Functions_R.Length;
            DoDialLeft = LStickNumFuncs > 1;
            DoDialRight = RStickNumFuncs > 1;
            DisableLeftDial_ = 0;
            DisableRightDial_ = 0;
            LStickFuncDegrees = 360 / Mathf.Max((float)LStickNumFuncs, 1);
            RStickFuncDegrees = 360 / Mathf.Max((float)RStickNumFuncs, 1);
            LStickFuncDegreesDivider = 1 / LStickFuncDegrees;
            RStickFuncDegreesDivider = 1 / RStickFuncDegrees;
            LStickNULL = new bool[LStickNumFuncs];
            RStickNULL = new bool[RStickNumFuncs];
            int u = 0;
            foreach (UdonSharpBehaviour usb in PassengerExtensions)
            {
                if (usb)
                {
                    usb.SetProgramVariable("PassengerFunctionsControl", this);
                    usb.SetProgramVariable("EntityControl", EntityControl);
                }
                u++;
            }
            u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_L)
            {
                if (!usb) { LStickNULL[u] = true; }
                else
                {
                    usb.SetProgramVariable("PassengerFunctionsControl", this);
                    usb.SetProgramVariable("EntityControl", EntityControl);
                }
                u++;
            }
            u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_R)
            {
                if (!usb) { RStickNULL[u] = true; }
                else
                {
                    usb.SetProgramVariable("PassengerFunctionsControl", this);
                    usb.SetProgramVariable("EntityControl", EntityControl);
                }
                u++;
            }
            DisableLeftDial_ = 0;
            DisableRightDial_ = 0;
            //work out angle to check against for function selection because straight up is the middle of a function (if *DialDivideStraightUp isn't true)
            Vector3 angle = new Vector3(0, 0, -1);
            if (LStickNumFuncs > 1)
            {
                if (LeftDialDivideStraightUp)
                {
                    LStickCheckAngle.x = 0;
                    LStickCheckAngle.y = -1;
                }
                else
                {
                    Vector3 LAngle = Quaternion.Euler(0, -((360 / LStickNumFuncs) / 2), 0) * angle;
                    LStickCheckAngle.x = LAngle.x;
                    LStickCheckAngle.y = LAngle.z;
                }
            }
            if (RStickNumFuncs > 1)
            {
                if (RightDialDivideStraightUp)
                {
                    RStickCheckAngle.x = 0;
                    RStickCheckAngle.y = -1;
                }
                else
                {
                    Vector3 RAngle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle;
                    RStickCheckAngle.x = RAngle.x;
                    RStickCheckAngle.y = RAngle.z;
                }
            }

            TellDFUNCsLR();

            SendEventToExtensions_Gunner("SFEXT_L_EntityStart");
        }
        public void SFEXT_O_PilotEnter()
        {
            InVR = EntityControl.InVR;
        }
        public void InVehicleControls()
        {
            if (!FunctionsActive) { return; }
            SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
            Vector2 LStickPos = Vector2.zero;
            Vector2 RStickPos = Vector2.zero;
            float LTrigger = 0;
            float RTrigger = 0;
            if (!InEditor)
            {
                LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            }

            //LStick Selection wheel
            if (DoDialLeft && !_DisableLeftDial)
            {
                if (InVR && LStickPos.magnitude > DialSensitivity)
                {
                    float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                    stickdir = -(stickdir - 180);
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * LStickFuncDegreesDivider, LStickNumFuncs - 1));
                    if (!LStickNULL[newselection])
                    { LStickSelection = newselection; }
                }
                if (LStickSelection != LStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (LStickSelectionLastFrame != -1 && Dial_Functions_L[LStickSelectionLastFrame] != null)
                    {
                        Dial_Functions_L[LStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (LStickSelection > -1)
                    {
                        if (Dial_Functions_L[LStickSelection] != null)
                        {
                            Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                    }
                    if (PlaySelectSoundLeft && SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (LStickDisplayHighlighter)
                    {
                        if (LStickSelection < 0)
                        { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
                        else
                        {
                            LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -LStickFuncDegrees * LStickSelection);
                        }
                    }
                    LStickSelectionLastFrame = LStickSelection;
                }
            }

            //RStick Selection wheel
            if (DoDialRight && !_DisableRightDial)
            {
                if (InVR && RStickPos.magnitude > DialSensitivity)
                {
                    float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                    stickdir = -(stickdir - 180);
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * RStickFuncDegreesDivider, RStickNumFuncs - 1));
                    if (!RStickNULL[newselection])
                    { RStickSelection = newselection; }
                }
                if (RStickSelection != RStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (RStickSelectionLastFrame != -1 && Dial_Functions_R[RStickSelectionLastFrame])
                    {
                        Dial_Functions_R[RStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (RStickSelection > -1)
                    {
                        if (Dial_Functions_R[RStickSelection])
                        {
                            Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                    }
                    if (PlaySelectSoundRight && SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (RStickDisplayHighlighter)
                    {
                        if (RStickSelection < 0)
                        { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
                        else
                        {
                            RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -RStickFuncDegrees * RStickSelection);
                        }
                    }
                    RStickSelectionLastFrame = RStickSelection;
                }
            }
        }

        public void TellDFUNCsLR()
        {
            for (int i = 0; i < Dial_Functions_L.Length; i++)
            {
                if (Dial_Functions_L[i])
                {
                    Dial_Functions_L[i].SetProgramVariable("LeftDial", true);
                    Dial_Functions_L[i].SetProgramVariable("DialPosition", i);
                }
            }
            for (int i = 0; i < Dial_Functions_R.Length; i++)
            {
                if (Dial_Functions_R[i])
                {
                    Dial_Functions_R[i].SetProgramVariable("LeftDial", false);
                    Dial_Functions_R[i].SetProgramVariable("DialPosition", i);
                }
            }
        }
        public void TakeOwnerShipOfExtensions()
        {
            if (!InEditor)
            {
                foreach (UdonSharpBehaviour EXT in PassengerExtensions)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            }
        }
        public void SendEventToExtensions_Gunner(string eventname)
        {
            foreach (UdonSharpBehaviour EXT in PassengerExtensions)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
        }
        public void UserEnterVehicleLocal()
        {
            LStickSelectionLastFrame = -1;
            RStickSelectionLastFrame = -1;
            Occupied = true;
            if (LStickNumFuncs == 1)
            {
                LStickSelection = 0;
                Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (RStickNumFuncs == 1)
            {
                LStickSelection = 0;
                Dial_Functions_R[LStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (RStickDisplayHighlighter) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (LStickDisplayHighlighter) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            TakeOwnerShipOfExtensions();
            FunctionsActive = true;
            if (!IsOwner)
            { IsOwner = true; SendEventToExtensions_Gunner("SFEXT_O_TakeOwnership"); }
            SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
            SendEventToExtensions_Gunner("SFEXT_O_PilotEnter");
        }
        public void UserEnterVehicleGlobal(VRCPlayerApi player)
        {
            Occupied = true;
            currentUser = player;
            if (!player.isLocal && IsOwner)
            {
                SendEventToExtensions_Gunner("SFEXT_O_LoseOwnership");
                IsOwner = false;
            }

            SendEventToExtensions_Gunner("SFEXT_G_PilotEnter");
        }
        public void UserExitVehicleLocal()
        {
            FunctionsActive = false;
            LStickSelection = -1;
            RStickSelection = -1;
            SendEventToExtensions_Gunner("SFEXT_O_PilotExit");
        }
        [NonSerialized] public bool pilotLeftFlag;
        public void UserExitVehicleGlobal()
        {
            currentUser = null;
            SendEventToExtensions_Gunner("SFEXT_G_PilotExit");
            Occupied = false;
            if (pilotLeftFlag)
            {
                SendCustomEventDelayedFrames(nameof(checkIfNewOwner), 1);
            }
        }
        public void checkIfNewOwner()
        {
            // all owned objects should be transferred to the same person when a player leaves so this should be fine
            if (localPlayer.IsOwner(Station.gameObject))
            {
                IsOwner = true; SendEventToExtensions_Gunner("SFEXT_O_TakeOwnership");
            }
        }

        public void ToggleStickSelectionLeft(UdonSharpBehaviour dfunc)
        {
            var index = Array.IndexOf(Dial_Functions_L, dfunc);
            if (LStickSelection == index)
            {
                LStickSelection = -1;
                dfunc.SendCustomEvent("DFUNC_Deselected");
            }
            else
            {
                LStickSelection = index;
                dfunc.SendCustomEvent("DFUNC_Selected");
            }
        }

        public void ToggleStickSelectionRight(UdonSharpBehaviour dfunc)
        {
            var index = Array.IndexOf(Dial_Functions_R, dfunc);
            if (RStickSelection == index)
            {
                RStickSelection = -1;
                dfunc.SendCustomEvent("DFUNC_Deselected");
            }
            else
            {
                RStickSelection = index;
                dfunc.SendCustomEvent("DFUNC_Selected");
            }
        }
    }
}