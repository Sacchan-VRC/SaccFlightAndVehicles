
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccEntitySendEvent : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [Tooltip("Name of entity event to send to the SaccEntity (send to all extensions)")]
        public string EntityEvent_Name = "SFEXT_O_RespawnButton";
        [Tooltip("Name of event to send to the SaccEntity (just sent to entity)")]
        public bool EntityEventGlobal = false;
        public UdonSharpBehaviour[] OtherScripts;
        public string OtherScripts_Event_Name;
        public bool OtherScript_EventGlobal = false;
        private bool BothGlobal;
        void Start()
        {
            if (EntityEvent_Name == string.Empty) { EntityEventGlobal = false; }
            if (OtherScripts_Event_Name == string.Empty) { OtherScript_EventGlobal = false; }
            if (EntityEventGlobal && OtherScript_EventGlobal) { BothGlobal = true; }
        }
        public override void Interact()
        {
            if (BothGlobal)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Event_both));
                return;
            }
            else
            {
                if (EntityEventGlobal)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EntityEvent));
                }
                else
                { EntityEvent(); }
                if (OtherScript_EventGlobal)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NormalEvent));
                }
                else
                { NormalEvent(); }
            }
        }
        public void Event_both()
        {
            if (EntityEvent_Name != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEvent_Name);
            }
            if (OtherScripts_Event_Name != string.Empty)
            {
                for (int i = 0; i < OtherScripts.Length; i++)
                {
                    OtherScripts[i].SendCustomEvent(OtherScripts_Event_Name);
                }
            }
        }
        public void EntityEvent()
        {
            if (EntityEvent_Name != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEvent_Name);
            }
        }
        public void NormalEvent()
        {
            if (OtherScripts_Event_Name != string.Empty)
            {
                for (int i = 0; i < OtherScripts.Length; i++)
                {
                    OtherScripts[i].SendCustomEvent(OtherScripts_Event_Name);
                }
            }
        }

        // can now be used as a DFUNC
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
        bool controlsActive = false;
        public void SFEXT_L_EntityStart() { Start(); }
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void ControlInputs()
        {
            if (!controlsActive) { return; }
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame)
                {
                    Interact();
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
            SendCustomEventDelayedFrames(nameof(ControlInputs), 1);
        }
        public void KeyboardInput()
        {
            Interact();
        }
        public void DFUNC_Deselected()
        {
            controlsActive = false;
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            if (!controlsActive)
            {
                controlsActive = true;
                ControlInputs();
            }
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            controlsActive = false;
        }
    }
}