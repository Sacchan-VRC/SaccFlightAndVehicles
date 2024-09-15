
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccEntitySendEvent : UdonSharpBehaviour
    {
        public UdonSharpBehaviour EntityControl;
        private SaccEntity EntityControl_;
        [Tooltip("Name of entity event to send to the SaccEntity (send to all extensions)")]
        public string EntityEvent_Name = "SFEXT_O_RespawnButton";
        [Tooltip("Name of event to send to the SaccEntity (just sent to entity)")]
        public bool EntityEventGlobal = false;
        public string Event_Name;
        public bool EventGlobal = false;
        private bool BothGlobal;
        bool isSaccEntity;
        void Start()
        {
            if (EntityEvent_Name == string.Empty) { EntityEventGlobal = false; }
            if (Event_Name == string.Empty) { EventGlobal = false; }
            if (EntityEventGlobal && EventGlobal) { BothGlobal = true; }
            if (EntityControl.GetUdonTypeName() == "SaccFlightAndVehicles.SaccEntity")
            {
                isSaccEntity = true;
                EntityControl_ = (SaccEntity)EntityControl;
            }
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
                if (EventGlobal)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NormalEvent));
                }
                else
                { NormalEvent(); }
            }
        }
        public void Event_both()
        {
            if (EntityEvent_Name != string.Empty && isSaccEntity)
            {
                EntityControl_.SendEventToExtensions(EntityEvent_Name);
            }
            if (Event_Name != string.Empty)
            {
                EntityControl.SendCustomEvent(Event_Name);
            }
        }
        public void EntityEvent()
        {
            if (EntityEvent_Name != string.Empty && isSaccEntity)
            {
                EntityControl_.SendEventToExtensions(EntityEvent_Name);
            }
        }
        public void NormalEvent()
        {
            if (Event_Name != string.Empty)
            {
                EntityControl.SendCustomEvent(Event_Name);
            }
        }

        // can now be used as a DFUNC
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
        bool controlsActive = false;
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