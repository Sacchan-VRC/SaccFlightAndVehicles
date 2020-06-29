
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccFlight : UdonSharpBehaviour
{
    private VRCPlayerApi localPlayer;
    public float flystr = .33f;
    public float BackThrustStrength = .5f;
    float controllertriggerR;
    float controllertriggerL;

    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }
    private void FixedUpdate()
    {
        if (!localPlayer.IsPlayerGrounded())//only does anything if in the air.
        {
            float Spacef = 0;
            if (Input.GetKey(KeyCode.Space)) { Spacef = 1; }
            float Ff = 0;
            if (Input.GetKey(KeyCode.F)) { Ff = 1; }

            controllertriggerR = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            controllertriggerL = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            if ((controllertriggerR > 0.1) || (Input.GetKey(KeyCode.F)))
            {
                Quaternion newspeed;
                Vector3 tempdir;
                Vector3 PlayerVel = localPlayer.GetVelocity();
                if (Input.GetKey(KeyCode.F))
                {
                    newspeed = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    tempdir = newspeed * (Vector3.forward);
                }
                else
                {
                    newspeed = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(90, 60, 0);
                    tempdir = newspeed * Vector3.up;
                }
                //get backwards amount
                float BackThrustAmount = Mathf.Clamp(((Vector3.Dot(PlayerVel, tempdir.normalized)) * BackThrustStrength) * -1, 0, 99999);

                tempdir = (tempdir * flystr) * Mathf.Max(controllertriggerR, Ff);
                float FinalCheatThrust = Mathf.Max(1, (BackThrustAmount * Mathf.Max(controllertriggerR, Ff)));
                localPlayer.SetVelocity(PlayerVel + tempdir * FinalCheatThrust);
            }
            if ((controllertriggerL > 0.1) || (Input.GetKey(KeyCode.Space)))
            {
                Vector3 tempdir = ((Vector3.up * flystr) * Mathf.Max(controllertriggerL, Spacef));
                localPlayer.SetVelocity(localPlayer.GetVelocity() + tempdir);
            }
        }
    }
}