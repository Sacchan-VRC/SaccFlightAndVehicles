
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccFlight : UdonSharpBehaviour
{
    private VRCPlayerApi localPlayer;
    public float ThrustStrength = .33f;
    public float BackThrustStrength = .5f;
    private float controllertriggerR;
    private float controllertriggerL;
    private bool InVR;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { gameObject.SetActive(false); }
        if (localPlayer.IsUserInVR())
        { InVR = true; }
    }
    private void FixedUpdate()
    {
        if (!localPlayer.IsPlayerGrounded())//only does anything if in the air.
        {
            float ForwardThrust = Mathf.Max(Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"), Input.GetKey(KeyCode.F) ? 1 : 0);
            float UpThrust = Mathf.Max(Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"), Input.GetKey(KeyCode.Space) ? 1 : 0);

            Vector3 PlayerVel = localPlayer.GetVelocity();

            Quaternion newrot;
            Vector3 NewForwardVec;
            if (InVR)
            {
                newrot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0);
                NewForwardVec = newrot * Vector3.forward;
            }
            else//Desktop
            {
                newrot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                NewForwardVec = newrot * (Vector3.forward);
            }
            //get backwards amount
            float BackThrustAmount = -((Vector3.Dot(PlayerVel, NewForwardVec)) * BackThrustStrength);
            NewForwardVec = NewForwardVec * ThrustStrength * ForwardThrust * Mathf.Max(1, (BackThrustAmount * ForwardThrust));

            Vector3 NewUpVec = ((Vector3.up * ThrustStrength) * UpThrust);

            localPlayer.SetVelocity(PlayerVel + NewForwardVec + NewUpVec);
        }
    }
}