
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccViewScreenButton : UdonSharpBehaviour
{
    public SaccViewScreenController ViewScreenControl;
    private VRCPlayerApi localPlayer;
    private bool InEditor = true;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InEditor = false; }
    }
    public override void Interact()
    {
        if (ViewScreenControl.Disabled)
        {
            ViewScreenControl.TurnOn();
        }
        else
        {
            if (!InEditor)
            {
                if (!localPlayer.IsOwner(ViewScreenControl.gameObject))
                { Networking.SetOwner(localPlayer, ViewScreenControl.gameObject); }
            }
            ViewScreenControl.AAMTarget++;
            if (ViewScreenControl.AAMTarget == ViewScreenControl.NumAAMTargets)
            {
                ViewScreenControl.AAMTarget = 0;
            }
            ViewScreenControl.RequestSerialization();
        }
    }
}
