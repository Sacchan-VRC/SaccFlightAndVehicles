
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccEntitySendEvent : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    [Tooltip("Name of event to send to the SaccEntity")]
    [SerializeField] private string EventName;
    private bool Global = false;
    private void Interact()
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
        EntityControl.SendEventToExtensions(EventName);
    }
}