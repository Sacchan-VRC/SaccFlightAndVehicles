
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
        public string Event_Name;
        public bool Global = false;
        public override void Interact()
        {
            if (Global)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Event));
            }
            else
            {
                Event();
            }
        }
        public void Event()
        {
            if (EntityEvent_Name != string.Empty)
            { EntityControl.SendEventToExtensions(EntityEvent_Name); }
            if (Event_Name != string.Empty)
            { EntityControl.SendCustomEvent(Event_Name); }
        }
    }
}