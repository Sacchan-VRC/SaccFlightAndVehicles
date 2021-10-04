
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_VTOLAngle : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    private bool UseLeftTrigger = false;
    private float VTOLDefault;
    private Transform VehicleTransform;
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
        VehicleTransform = ((SaccEntity)SAVControl.GetProgramVariable("EntityControl")).GetComponent<Transform>();
        localPlayer = Networking.LocalPlayer;
        ThrottleSensitivity = (float)SAVControl.GetProgramVariable("ThrottleSensitivity");
        SAVControl.SetProgramVariable("VTOLenabled", true);
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        TriggerLastFrame = true;
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotExit()
    {
        TriggerLastFrame = true;
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
            Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            handpos = VehicleTransform.InverseTransformDirection(handpos);

            if (!TriggerLastFrame)
            {
                VTOLZeroPoint = handpos.z;
                VTOLTemp = (float)SAVControl.GetProgramVariable("VTOLAngle");
            }
            float VTOLAngleDifference = (VTOLZeroPoint - handpos.z) * -ThrottleSensitivity;
            SAVControl.SetProgramVariable("VTOLAngleInput", Mathf.Clamp(VTOLTemp + VTOLAngleDifference, 0, 1));

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
}
