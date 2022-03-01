
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DFUNC_VTOLAngle : UdonSharpBehaviour
{
    public UdonSharpBehaviour SAVControl;
    private bool UseLeftTrigger = false;
    private float VTOLDefault;
    private Transform ControlsRoot;
    private VRCPlayerApi localPlayer;
    private bool TriggerLastFrame;
    private float VTOLTemp;
    private float VTOLZeroPoint;
    private float ThrottleSensitivity;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        VTOLDefault = (float)SAVControl.GetProgramVariable("VTOLDefaultValue");
        ControlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
        localPlayer = Networking.LocalPlayer;
        ThrottleSensitivity = (float)SAVControl.GetProgramVariable("ThrottleSensitivity");
        SAVControl.SetProgramVariable("VTOLenabled", true);
    }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = false;
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
        if (Trigger > 0.75)
        {
            Vector3 handpos = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            handpos = ControlsRoot.InverseTransformDirection(handpos);

            if (!TriggerLastFrame)
            {
                VTOLZeroPoint = handpos.z;
                VTOLTemp = (float)SAVControl.GetProgramVariable("VTOLAngle");
            }
            float VTOLAngleDifference = (VTOLZeroPoint - handpos.z) * -ThrottleSensitivity;
            VTOLTemp += VTOLAngleDifference;
            if (VTOLTemp > 0)
            { VTOLTemp = VTOLTemp - Mathf.Floor(VTOLTemp); }
            else
            { VTOLTemp = 1 - (Mathf.Abs(VTOLTemp) - Mathf.Ceil(VTOLTemp)); }
            SAVControl.SetProgramVariable("VTOLAngleInput", VTOLTemp);

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
}
