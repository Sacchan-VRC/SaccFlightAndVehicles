
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
        public string Event_Name;
        public bool EventGlobal = false;
        private bool BothGlobal;
        void Start()
        {
            if (EntityEvent_Name == string.Empty) { EntityEventGlobal = false; }
            if (Event_Name == string.Empty) { EventGlobal = false; }
            if (EntityEventGlobal && EventGlobal) { BothGlobal = true; }
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
            if (EntityEvent_Name != string.Empty)
            { EntityControl.SendEventToExtensions(EntityEvent_Name); }
            if (Event_Name != string.Empty)
            { EntityControl.SendCustomEvent(Event_Name); }
        }
        public void EntityEvent()
        {
            if (EntityEvent_Name != string.Empty)
            { EntityControl.SendEventToExtensions(EntityEvent_Name); }
        }
        public void NormalEvent()
        {
            if (Event_Name != string.Empty)
            { EntityControl.SendCustomEvent(Event_Name); }
        }
    }
}