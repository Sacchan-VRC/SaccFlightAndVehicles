
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SAV_PassengerFunctionsController : UdonSharpBehaviour
{
    public UdonSharpBehaviour[] Dial_Functions_L;
    public UdonSharpBehaviour[] Dial_Functions_R;
    public UdonSharpBehaviour[] PassengerExtensions;
    [SerializeField] private Transform LStickDisplayHighlighter;
    [SerializeField] private Transform RStickDisplayHighlighter;
    private bool LStickDisplayHighlighterNULL;
    private bool RStickDisplayHighlighterNULL;
    private UdonSharpBehaviour CurrentSelectedFunctionL;
    private UdonSharpBehaviour CurrentSelectedFunctionR;
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
    private int Lstickselection_STRING = Animator.StringToHash("Lstickselection");
    private int Rstickselection_STRING = Animator.StringToHash("Rstickselection");
    private bool FunctionsActive = false;
    private bool LeftDialOnlyOne;
    private bool RightDialOnlyOne;
    private bool LeftDialEmpty;
    private bool RightDialEmpty;
    private bool DoFuncsL;
    private bool DoFuncsR;
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
            u++;
        }
        u = 0;
        foreach (UdonSharpBehaviour usb in Dial_Functions_R)
        {
            if (usb == null) { RStickNULL[u] = true; }
            u++;
        }
        if (LStickNumFuncs == 1) { LeftDialOnlyOne = true; }
        if (RStickNumFuncs == 1) { RightDialOnlyOne = true; }
        if (LStickNumFuncs == 0) { LeftDialEmpty = true; }
        if (RStickNumFuncs == 0) { RightDialEmpty = true; }
        if (LeftDialEmpty || LeftDialOnlyOne) { DoFuncsL = false; } else { DoFuncsL = true; }
        if (RightDialEmpty || RightDialOnlyOne) { DoFuncsR = false; } else { DoFuncsR = true; }
        //work out angle to check against for function selection because straight up is the middle of a function
        Vector3 angle = new Vector3(0, 0, -1);
        if (!LeftDialEmpty) { angle = Quaternion.Euler(0, -((360 / LStickNumFuncs) / 2), 0) * angle; }
        LStickCheckAngle.x = angle.x;
        LStickCheckAngle.y = angle.z;

        angle = new Vector3(0, 0, -1);
        if (!RightDialEmpty) { angle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle; }
        RStickCheckAngle.x = angle.x;
        RStickCheckAngle.y = angle.z;

        LStickDisplayHighlighterNULL = LStickDisplayHighlighter == null;
        RStickDisplayHighlighterNULL = RStickDisplayHighlighter == null;

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
        if (DoFuncsL)
        {
            //LStick Selection wheel
            if (InVR && LStickPos.magnitude > .7f)
            {
                float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                //R stick value is manually synced using events because i don't want to use too many synced variables.
                //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                stickdir = (stickdir - 180) * -1;
                int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / LStickFuncDegrees, LStickNumFuncs - 1));
                if (!LStickNULL[newselection])
                { LStickSelection = newselection; }
                //doing this in DFUNC scripts that need it instead so that we send less events
                /*             if (VehicleAnimator.GetInteger(Lstickselection_STRING) != LStickSelection)
                            {
                                LStickSetAnimatorInt();
                            } */
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
                    else { CurrentSelectedFunctionL = null; }
                }
            }
            if (LStickSelection != LStickSelectionLastFrame)
            {
                if (LStickSelection < 0)
                { if (!LStickDisplayHighlighterNULL) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); } }
                else
                {
                    if (!LStickDisplayHighlighterNULL) { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, LStickFuncDegrees * LStickSelection, 0); }
                }
            }
            LStickSelectionLastFrame = LStickSelection;
        }

        if (DoFuncsR)
        {
            //RStick Selection wheel
            if (InVR && RStickPos.magnitude > .7f)
            {
                float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                //R stick value is manually synced using events because i don't want to use too many synced variables.
                //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                stickdir = (stickdir - 180) * -1;
                int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / RStickFuncDegrees, RStickNumFuncs - 1));
                if (!RStickNULL[newselection])
                { RStickSelection = newselection; }
                //doing this in DFUNC scripts that need it instead so that we send less events
                /*             if (VehicleAnimator.GetInteger(Rstickselection_STRING) != RStickSelection)
                            {
                                RStickSetAnimatorInt();
                            } */
            }
            if (RStickSelection != RStickSelectionLastFrame)
            {
                //new function selected, send deselected to old one
                if (RStickSelectionLastFrame != -1 && Dial_Functions_R[RStickSelectionLastFrame] != null)
                {
                    Dial_Functions_R[RStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                }
                //get udonbehaviour for newly selected function and then send selected
                if (RStickSelection > -1)
                {
                    if (Dial_Functions_R[RStickSelection] != null)
                    {
                        Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
                    }
                    else { CurrentSelectedFunctionR = null; }
                }
            }
            if (RStickSelection != RStickSelectionLastFrame)
            {
                if (LStickSelection < 0)
                { if (!RStickDisplayHighlighterNULL) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); } }
                else
                {
                    if (!RStickDisplayHighlighterNULL) { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, RStickFuncDegrees * RStickSelection, 0); }
                }
            }
            RStickSelectionLastFrame = RStickSelection;
        }
    }

    public void TellDFUNCsLR()
    {
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_LeftDial"); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_RightDial"); }
        }
    }
    public void TakeOwnerShipOfExtensions()
    {
        if (!InEditor)
        {
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in PassengerExtensions)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
        }
    }
    public void SendEventToExtensions_Gunner(string eventname)
    {
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in PassengerExtensions)
        {
            if (EXT != null)
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
        TakeOwnerShipOfExtensions();
        FunctionsActive = true;
    }
    private void OnDisable()
    {
        if (LeftDialOnlyOne)
        {
            Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Deselected");
        }
        if (RightDialOnlyOne)
        {
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
        else
        {
            SendEventToExtensions_Gunner("SFEXTP_O_PassengerExit");
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
    public void SFEXT_G_RespawnButton()
    {
        SendEventToExtensions_Gunner("SFEXTP_G_RespawnButton");
    }
    public void SFEXT_G_TouchDown()
    {
        SendEventToExtensions_Gunner("SFEXTP_G_TouchDown");
    }
    public void SFEXT_G_TakeOff()
    {
        SendEventToExtensions_Gunner("SFEXTP_G_TakeOff");
    }
    public void SFEXT_G_PassengerEnter()
    {
        SendEventToExtensions_Gunner("SFEXTP_G_PassengerEnter");
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
}