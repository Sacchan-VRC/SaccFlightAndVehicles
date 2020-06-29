
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
    public Camera AACam;
    Vector3 temprot;
    private float RstickV;
    private void Update()
    {
        RstickV = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
        if (Mathf.Abs(RstickV) > .1)
        {
            AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - .5f * RstickV, 1, 90);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - .3f, 1, 90);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView + .3f, 1, 90);
        }

        //Heading indicator
        temprot = AAGunControl.Rotator.transform.rotation.eulerAngles;
        temprot.x = 0;
        temprot.z = 0;
        HeadingIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////

        //Elevation indicator
        temprot = AAGunControl.Rotator.transform.localRotation.eulerAngles;
        temprot.y = 0;
        temprot.z = 0;
        ElevationIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////
    }
}