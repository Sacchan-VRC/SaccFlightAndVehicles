
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class HUDControllerAAGun : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    Vector3 temprot;
    private void Update()
    {
        //Heading indicator
        temprot = AAGunControl.PitchRotator.transform.rotation.eulerAngles;
        temprot.x = 0;
        temprot.z = 0;
        HeadingIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////

        //Elevation indicator
        temprot = AAGunControl.PitchRotator.transform.localRotation.eulerAngles;
        temprot.y = 0;
        temprot.z = 0;
        ElevationIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////
    }
}