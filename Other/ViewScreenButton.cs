
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ViewScreenButton : UdonSharpBehaviour
{
    public ViewScreenController ViewScreenControl;
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
            Networking.SetOwner(ViewScreenControl.localPlayer, ViewScreenControl.gameObject);
            ViewScreenControl.AAMTarget++;
            if (ViewScreenControl.AAMTarget > ViewScreenControl.NumAAMTargets - 1)
            {
                ViewScreenControl.AAMTarget = 0;
            }
        }
    }
}
