
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_RadioPTT : UdonSharpBehaviour
    {
        [Header("PTT Can only be used by the owner of the SAV_Radio\nIf you want more than one person to be able to use PTT\n you must have another SAV_Radio in their PassengerFunctions")]
        [SerializeField] SAV_Radio Radio;
        [SerializeField] KeyCode PTT_Key;
        [Tooltip("Press to Toggle mic instead of push to talk?")]
        [SerializeField] bool ToggleMode = false;
        [SerializeField] bool Toggle_DefaultOn = true;
        [Tooltip("If on a pickup: Use VRChat's OnPickupUseDown functionality")]
        [SerializeField] bool use_OnPickupUseDown = false;
        [Header("FUNCONs are controlled by SAV_Radio")]
        [System.NonSerializedAttribute] public GameObject Dial_Funcon;// here so that the function dial generator makes a funcon for it
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool TriggerLastFrame;
        private bool InVR;
        private bool Piloting;
        public void SFEXT_L_EntityStart()
        {
            InVR = EntityControl.InVR;
            Radio.PTT_MODE = true;
        }
        bool Selected;
        private void Update()
        {
            if (!Piloting || !Selected && InVR) return;
            float Trigger;
            if (use_OnPickupUseDown)
                Trigger = PickupTrigger;
            else
            {
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            }
            if (Trigger > 0.75 || Input.GetKey(PTT_Key))
            {
                if (!TriggerLastFrame)
                {
                    if (ToggleMode)
                    {
                        PTT_Toggle();
                    }
                    else
                    {
                        SET_PTT_ON();
                    }
                    TriggerLastFrame = true;
                }
            }
            else
            {
                if (!ToggleMode && TriggerLastFrame)
                {
                    SET_PTT_OFF();
                }
                TriggerLastFrame = false;
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
            TriggerLastFrame = true;
        }
        public void DFUNC_Deselected()
        {
            PickupTrigger = 0;
            Selected = false;
        }
        void PTT_Toggle()
        {
            if (!Radio.PTT_ACTIVE)
            {
                SET_PTT_ON();
            }
            else
            {
                SET_PTT_OFF();
            }
        }
        public void SET_PTT_ON()
        {
            if (Networking.LocalPlayer.IsOwner(Radio.gameObject))
                Radio.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Self, "Call_PTT_ON");
            else
                Radio.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Call_PTT_ON");
        }
        void SET_PTT_OFF()
        {
            if (Networking.LocalPlayer.IsOwner(Radio.gameObject))
                Radio.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Self, "Call_PTT_OFF");
            else
                Radio.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Call_PTT_OFF");
        }
        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            InVR = EntityControl.InVR;
            Piloting = true;
            if (ToggleMode && Toggle_DefaultOn)
            { SendCustomEventDelayedFrames(nameof(SET_PTT_ON), 15); }//ensure SAV_Radio's SFEXT_O_PilotEnter() has run first
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            PickupTrigger = 0;
            Piloting = false;
            SET_PTT_OFF();
        }
        public void SFEXT_O_OnPickup()
        {
            SFEXT_O_PilotEnter();
        }
        public void SFEXT_O_OnDrop()
        {
            SFEXT_O_PilotExit();
        }
        private int PickupTrigger = 0;
        public void SFEXT_O_OnPickupUseDown()
        {
            PickupTrigger = 1;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            PickupTrigger = 0;
        }
    }
}