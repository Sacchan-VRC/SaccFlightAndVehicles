
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Canopy : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public UdonSharpBehaviour SoundControl;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public Animator CanopyAnimator;
        [Tooltip("How long to wait before telling the sound controller to change the sounds to outside vehicle sounds when opening")]
        public float CanopyOpenTime = 0f;
        [Tooltip("The length of the canopy close animation, or how long to wait before telling the sound controller to change the sounds to inside vehicle sounds when closing")]
        public float CanopyCloseTime = 1.8f;
        [Tooltip("Seats whos sound to change when opening canopy. Leave empty to effect all seats")]
        public SaccVehicleSeat[] EffectedSeats;
        [Tooltip("The canopy can break off? Requires animation setup")]
        public bool CanopyCanBreakOff = false;
        [Header("Meters/s")]
        [Tooltip("Speed at which canopy will break off if it's still open")]
        public float CanopyBreakSpeed = 50;
        [Tooltip("Speed at which canopy will close itself (useful for noobs/lazy people)")]
        public float CanopyAutoCloseSpeed = 20;
        [Tooltip("Extra drag applied to vehicle while canopy is open")]
        public float CanopyDragMulti = 1.2f;
        [Tooltip("Name of animator boolean that is true when canopy is open")]
        public string AnimCanopyBool = "canopyopen";
        [Tooltip("Name of animator boolean that is true when canopy is broken")]
        public string AnimCanopyBroken = "canopybroken";
        public bool DoCanopyOpenDrag = true;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        private Transform VehicleTransform;
        private VRCPlayerApi localPlayer;
        private SAV_HUDController HUDControl;
        private bool InVR;
        [System.NonSerializedAttribute] public bool CanopyOpen;
        [System.NonSerializedAttribute] public bool CanopyBroken;
        private bool Selected;
        private float LastCanopyToggleTime = -999;
        private bool DragApplied;
        private bool CanopyTransitioning = false;
        private bool InEditor = true;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            VehicleTransform = EntityControl.transform;
            CanopyDragMulti -= 1;
            //crashes if not sent delayed because the order of events sent by SendCustomEvent are not maintained, (SaccEntity.SendEventToExtensions())
            SendCustomEventDelayedSeconds(nameof(CanopyOpening), 10);
        }
        public void SFEXT_L_OnEnable()
        {
            if (CanopyAnimator) { { CanopyAnimator.SetBool(AnimCanopyBool, CanopyOpen); } }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            if (Dial_Funcon) { Dial_Funcon.SetActive(CanopyOpen); }
            InVR = EntityControl.InVR;
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            Selected = false;
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(CanopyOpen); }
        }
        public void SFEXT_G_Explode()
        {
            RepairCanopy();
            if (!CanopyOpen) { CanopyOpening(); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (CanopyBroken)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyBreakOff)); }
            else if (!CanopyOpen)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyClosing)); }
        }
        public void SFEXT_G_RespawnButton()
        {
            CanopyBroken = false;
            if (CanopyAnimator) { CanopyAnimator.SetBool(AnimCanopyBroken, false); }
            if (!CanopyOpen) CanopyOpening();
        }
        public void SFEXT_G_ReSupply()
        {
            if (CanopyBroken)
            {
                if ((float)SAVControl.GetProgramVariable("Health") == (float)SAVControl.GetProgramVariable("FullHealth"))
                {
                    RepairCanopy();
                }
            }
        }
        public void SFEXT_G_RePair() { SFEXT_G_ReSupply(); }
        public void RepairCanopy()
        {
            CanopyBroken = false;
            if (CanopyAnimator) { CanopyAnimator.SetBool(AnimCanopyBroken, false); }
            if (EntityControl.IsOwner) { SendCustomEventDelayedFrames(nameof(SendCanopyRepair), 1); }
            if (CanopyOpen) { CanopyClosing(); }
        }
        private void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75)
                {
                    if (!TriggerLastFrame)
                    {
                        ToggleCanopy();
                    }
                    TriggerLastFrame = true;
                }
                else
                {
                    TriggerLastFrame = false;
                }
            }

            if (!CanopyBroken && CanopyOpen && !EntityControl._dead)
            {
                if (CanopyCanBreakOff && (float)SAVControl.GetProgramVariable("AirSpeed") > CanopyBreakSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyBreakOff));
                }
                else if ((float)SAVControl.GetProgramVariable("AirSpeed") > CanopyAutoCloseSpeed && (Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f)//.1f is extra delay to match the animator because it's using write defaults off
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyClosing));
                }
            }
        }
        public void CanopyOpening()
        {
            if (CanopyOpen || CanopyBroken) { return; }
            LastCanopyToggleTime = Time.time;
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            CanopyOpen = true;
            if (CanopyAnimator) { CanopyAnimator.SetBool(AnimCanopyBool, true); }

            if (!DragApplied && DoCanopyOpenDrag)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + CanopyDragMulti);
                DragApplied = true;
            }

            if (EffectedSeats.Length == 0)
            {
                for (int i = 0; i < EntityControl.VehicleSeats.Length; i++)
                {
                    EntityControl.VehicleSeats[i].SetProgramVariable("numOpenDoors", (int)EntityControl.VehicleSeats[i].GetProgramVariable("numOpenDoors") + 1);
                }
            }
            else
            {
                for (int i = 0; i < EffectedSeats.Length; i++)
                {
                    EffectedSeats[i].SetProgramVariable("numOpenDoors", (int)EffectedSeats[i].GetProgramVariable("numOpenDoors") + 1);
                }
            }
            SoundControl.SendCustomEventDelayedSeconds("UpdateDoorsOpen", CanopyOpenTime);
            if (EntityControl.IsOwner) { SendCanopyOpened(); }
        }
        public void CanopyClosing()
        {
            if (!CanopyOpen || CanopyBroken) { return; }//don't bother when not necessary (OnPlayerJoined() wasn't you)
            LastCanopyToggleTime = Time.time;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            CanopyOpen = false;
            if (CanopyAnimator) { CanopyAnimator.SetBool(AnimCanopyBool, false); }
            CanopyTransitioning = true;
            SoundControl.SendCustomEventDelayedSeconds("DoorClose", CanopyCloseTime);
            SendCustomEventDelayedSeconds("SetCanopyTransitioningFalse", CanopyCloseTime);

            if (DragApplied)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - CanopyDragMulti);
                DragApplied = false;
            }
            if (EffectedSeats.Length == 0)
            {
                for (int i = 0; i < EntityControl.VehicleSeats.Length; i++)
                {
                    EntityControl.VehicleSeats[i].SetProgramVariable("numOpenDoors", (int)EntityControl.VehicleSeats[i].GetProgramVariable("numOpenDoors") - 1);
                }
            }
            else
            {
                for (int i = 0; i < EffectedSeats.Length; i++)
                {
                    EffectedSeats[i].SetProgramVariable("numOpenDoors", (int)EffectedSeats[i].GetProgramVariable("numOpenDoors") - 1);
                }
            }
            SoundControl.SendCustomEventDelayedSeconds("UpdateDoorsOpen", CanopyCloseTime);
            if (EntityControl.IsOwner) { SendCanopyClosed(); }
        }
        //these events have to be used with a frame delay because if you call them from an event that was called by the same SendEventToExtensions function, the previous call stops.
        public void SendCanopyClosed()
        {
            EntityControl.SendEventToExtensions("SFEXT_O_CanopyClosed");
        }
        public void SendCanopyOpened()
        {
            EntityControl.SendEventToExtensions("SFEXT_O_CanopyOpen");
        }
        public void SendCanopyBreak()
        {
            EntityControl.SendEventToExtensions("SFEXT_O_CanopyBreak");
        }
        public void SendCanopyRepair()
        {
            EntityControl.SendEventToExtensions("SFEXT_O_CanopyRepair");
        }
        public void SetCanopyTransitioningFalse()
        {
            CanopyTransitioning = false;
        }
        public void CanopyBreakOff()
        {
            if (CanopyBroken) { return; }
            if (Dial_Funcon) Dial_Funcon.SetActive(true);
            CanopyOpen = true;
            CanopyBroken = true;
            if (CanopyAnimator) { CanopyAnimator.SetBool(AnimCanopyBroken, true); }
            if (EntityControl.IsOwner)
            {
                SendCustomEventDelayedFrames(nameof(SendCanopyBreak), 1);
            }
            if (!DragApplied && DoCanopyOpenDrag)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + CanopyDragMulti);
                DragApplied = true;
            }
        }
        public void ToggleCanopy()
        {
            if ((Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f && !CanopyBroken && !CanopyTransitioning && !(!CanopyCanBreakOff && (float)SAVControl.GetProgramVariable("Speed") > CanopyAutoCloseSpeed))
            {
                if (CanopyOpen)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyClosing));
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CanopyOpening));
                }
            }
        }
        public void KeyboardInput()
        {
            ToggleCanopy();
        }
    }
}