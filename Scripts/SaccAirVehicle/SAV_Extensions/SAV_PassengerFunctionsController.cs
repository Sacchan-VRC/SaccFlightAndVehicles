
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
        [Tooltip("Oneshot sound played when switching functions")]
        public AudioSource SwitchFunctionSound;
        private int LStickNumFuncs;
        private int RStickNumFuncs;
        private float LStickFuncDegrees;
        private float RStickFuncDegrees;
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
        private bool FunctionsActive = false;
        private bool LeftDialOnlyOne;
        private bool RightDialOnlyOne;
        private bool LeftDialEmpty;
        private bool RightDialEmpty;
        private bool LStickDoDial;
        private bool RStickDoDial;
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
            if (localPlayer != null)
            {
                InEditor = false;
                InVR = localPlayer.IsUserInVR();
            }

            LStickNumFuncs = Dial_Functions_L.Length;
            RStickNumFuncs = Dial_Functions_R.Length;
            LStickFuncDegrees = 360 / (float)LStickNumFuncs;
            RStickFuncDegrees = 360 / (float)RStickNumFuncs;
            LStickNULL = new bool[LStickNumFuncs];
            RStickNULL = new bool[RStickNumFuncs];
            int u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_L)
            {
                if (usb == null) { LStickNULL[u] = true; }
                else usb.SetProgramVariable("PassengerFunctionsController", this);
                u++;
            }
            u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_R)
            {
                if (usb == null) { RStickNULL[u] = true; }
                else usb.SetProgramVariable("PassengerFunctionsController", this);
                u++;
            }
            if (LStickNumFuncs == 1) { LeftDialOnlyOne = true; }
            if (RStickNumFuncs == 1) { RightDialOnlyOne = true; }
            if (LStickNumFuncs == 0) { LeftDialEmpty = true; }
            if (RStickNumFuncs == 0) { RightDialEmpty = true; }
            if (LeftDialEmpty || LeftDialOnlyOne) { LStickDoDial = false; } else { LStickDoDial = true; }
            if (RightDialEmpty || RightDialOnlyOne) { RStickDoDial = false; } else { RStickDoDial = true; }
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
            SendEventToExtensions_Gunner("SFEXTP_L_EntityStart");
        }
        private void Update()
        {

            Vector2 RStickPos = new Vector2(0, 0);
            Vector2 LStickPos = new Vector2(0, 0);
            LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            if (LStickDoDial && !_DisableLeftDial)
            {
                //LStick Selection wheel
                if (InVR && LStickPos.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                    stickdir = (stickdir - 180) * -1;
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / LStickFuncDegrees, LStickNumFuncs - 1));
                    if (!LStickNULL[newselection])
                    { LStickSelection = newselection; }
                }
                if (LStickSelection != LStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (LStickSelectionLastFrame != -1 && Dial_Functions_L[LStickSelectionLastFrame])
                    {
                        Dial_Functions_L[LStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (LStickSelection > -1)
                    {
                        if (Dial_Functions_L[LStickSelection])
                        {
                            Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                    }

                    if (SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (LStickSelection < 0)
                    { if (LStickDisplayHighlighter) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); } }
                    else
                    {
                        if (LStickDisplayHighlighter) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, LStickFuncDegrees * LStickSelection); }
                    }
                }
                LStickSelectionLastFrame = LStickSelection;
            }

            if (RStickDoDial && !_DisableRightDial)
            {
                //RStick Selection wheel
                if (InVR && RStickPos.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                    stickdir = (stickdir - 180) * -1;
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / RStickFuncDegrees, RStickNumFuncs - 1));
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

                    if (SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (RStickSelection < 0)
                    { if (RStickDisplayHighlighter) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); } }
                    else
                    {
                        if (RStickDisplayHighlighter) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, RStickFuncDegrees * RStickSelection); }
                    }
                }
                RStickSelectionLastFrame = RStickSelection;
            }
        }

        public void TellDFUNCsLR()
        {
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            {
                if (EXT)
                { EXT.SendCustomEvent("DFUNC_LeftDial"); }
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            {
                if (EXT)
                { EXT.SendCustomEvent("DFUNC_RightDial"); }
            }
        }
        public void TakeOwnerShipOfExtensions()
        {
            if (!InEditor)
            {
                foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in PassengerExtensions)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            }
        }
        public void SendEventToExtensions_Gunner(string eventname)
        {
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
            foreach (UdonSharpBehaviour EXT in PassengerExtensions)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
        }
        private void OnEnable()
        {
            LStickSelectionLastFrame = -1;
            RStickSelectionLastFrame = -1;
            if (LeftDialOnlyOne)
            {
                LStickSelection = 0;
                Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (RightDialOnlyOne)
            {
                RStickSelection = 0;
                Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (RStickDisplayHighlighter) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (LStickDisplayHighlighter) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            TakeOwnerShipOfExtensions();
            FunctionsActive = true;
        }
        private void OnDisable()
        {
            if (LeftDialOnlyOne)
            {
                LStickSelection = 0;
                Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Deselected");
            }
            if (RightDialOnlyOne)
            {
                RStickSelection = 0;
                Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Deselected");
            }
            LStickSelection = -1;
            RStickSelection = -1;
        }
        public void SFEXT_P_PassengerEnter()
        {
            SendCustomEventDelayedFrames(nameof(PassengerEnter_2), 2);
        }
        public void PassengerEnter_2()//this shouldn't be needed but it is. OnEnable seems to run late
        {
            if (FunctionsActive)//only do this for the one in the seat that has activated it
            {
                SendEventToExtensions_Gunner("SFEXTP_O_UserEnter");
            }
            else
            {
                SendEventToExtensions_Gunner("SFEXTP_P_PassengerEnter");
            }
        }
        public void SFEXT_P_PassengerExit()
        {
            if (FunctionsActive)
            {
                FunctionsActive = false;
                SendEventToExtensions_Gunner("SFEXTP_O_UserExit");
            }
        }
        public void SFEXT_G_ReSupply()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_ReSupply");
        }
        public void SFEXT_G_Explode()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_Explode");
        }
        public void SFEXT_G_ReAppear()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_ReAppear");
        }
        public void SFEXT_G_RespawnButton()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_RespawnButton");
        }
        public void SFEXT_G_TouchDown()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_TouchDown");
        }
        public void SFEXT_G_TouchDownWater()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_TouchDownWater");
        }
        public void SFEXT_G_TakeOff()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_TakeOff");
        }
        public void SFEXT_G_PassengerEnter()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_PassengerEnter");
        }
        public void SFEXT_G_PilotEnter()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_PilotEnter");
        }
        public void SFEXT_G_PilotExit()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_PilotExit");
        }
        public void SFEXT_G_PassengerExit()
        {
            SendEventToExtensions_Gunner("SFEXTP_G_PassengerExit");
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (FunctionsActive)
            { SendEventToExtensions_Gunner("SFEXTP_O_PlayerJoined"); }
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