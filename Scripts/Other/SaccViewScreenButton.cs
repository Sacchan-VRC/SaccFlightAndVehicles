
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccViewScreenButton : UdonSharpBehaviour
{
    public SaccViewScreenController ViewScreenControl;
    void Interact()
    {
        if (ViewScreenControl.Disabled && ViewScreenControl.NumAAMTargets > 0)
        {
            ViewScreenControl.PlaneCamera.gameObject.SetActive(true);
            ViewScreenControl.ViewScreen.gameObject.SetActive(true);
            ViewScreenControl.Disabled = false;
            ViewScreenControl.PlaneCamera.transform.rotation = ViewScreenControl.AAMTargets[ViewScreenControl.AAMTarget].transform.rotation;
        }
        else
        {
            if (!ViewScreenControl.InEditor)
            { Networking.SetOwner(ViewScreenControl.localPlayer, ViewScreenControl.gameObject); }

            ViewScreenControl.AAMTarget++;
            if (ViewScreenControl.AAMTarget > ViewScreenControl.NumAAMTargets - 1)
            {
                ViewScreenControl.AAMTarget = 0;
            }
        }
    }
}
