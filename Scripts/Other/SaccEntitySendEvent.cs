
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccEntitySendEvent : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    [SerializeField] private string EventName;
    private bool Global = false;
    private void Interact()
    {
        if (Global)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EventGlobal));
        }
        else
        {
            EntityControl.SendEventToExtensions(EventName);
        }
    }
    public void EventGlobal()
    {
        EntityControl.SendEventToExtensions(EventName);
    }
}