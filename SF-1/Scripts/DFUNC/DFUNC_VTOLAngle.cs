
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_VTOLAngle : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] EngineController EngineControl;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private bool TriggerLastFrame;
    private float VTOLTemp;
    private float VTOLZeroPoint;
    private float ThrottleSensitivity;
    public void SFEXT_L_ECStart()
    {
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        localPlayer = Networking.LocalPlayer;
        ThrottleSensitivity = EngineControl.ThrottleSensitivity;
    }
    public void DFUNC_Selected()
    {
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
            Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            handpos = VehicleTransform.InverseTransformDirection(handpos);

            if (!TriggerLastFrame)
            {
                VTOLZeroPoint = handpos.z;
                VTOLTemp = EngineControl.VTOLAngle;
            }
            float VTOLAngleDifference = (VTOLZeroPoint - handpos.z) * -ThrottleSensitivity;
            EngineControl.VTOLAngleInput = Mathf.Clamp(VTOLTemp + VTOLAngleDifference, 0, 1);

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
}
