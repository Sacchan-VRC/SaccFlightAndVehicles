
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
        public GameObject Dial_Funcon;
        [Tooltip("Press to Toggle mic instead of push to talk?")]
        [SerializeField] bool ToggleMode = false;
        [Tooltip("If on a pickup: Use VRChat's OnPickupUseDown functionality")]
        [SerializeField] bool use_OnPickupUseDown = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        private bool InVR;
        private bool Piloting;
        public void SFEXT_L_EntityStart()
        {
            InVR = EntityControl.InVR;
            Radio.PTT_MODE = true;
            Radio.PTTControl = this;
            if (Dial_Funcon) Dial_Funcon.SetActive(false);
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
                        PTT_ON();
                    }
                }
                TriggerLastFrame = true;
            }
            else
            {
                if (!ToggleMode && TriggerLastFrame)
                {
                    PTT_OFF();
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
        [System.NonSerialized] public bool PTT_ACTIVE;
        void PTT_Toggle()
        {
            if (!PTT_ACTIVE)
            {
                PTT_ON();
            }
            else
            {
                PTT_OFF();
            }
        }
        void PTT_ON()
        {
            if (Radio.Channel == 0) return; // 0 means Radio off
            if (!PTT_ACTIVE)
            {
                if (Radio.Channel < 200) return; // should never be true but will prevent crash if it is (byte)
                PTT_ACTIVE = true;
                Radio.Channel -= 200;
                Radio.RequestSerialization();
                if (Dial_Funcon) Dial_Funcon.SetActive(true);
            }
        }
        void PTT_OFF()
        {
            if (PTT_ACTIVE)
            {
                if (Radio.Channel > 55) return;
                PTT_ACTIVE = false;
                Radio.Channel += 200;
                Radio.RequestSerialization();
                if (Dial_Funcon) Dial_Funcon.SetActive(false);
            }
        }
        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            InVR = EntityControl.InVR;
            Piloting = true;
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            PickupTrigger = 0;
            Piloting = false;
            PTT_OFF();
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