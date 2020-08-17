
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
            bool GetkeySpace = Input.GetKey(KeyCode.Space);
            bool GetkeyF = Input.GetKey(KeyCode.F);
            float Ff = GetkeyF ? 1 : 0;

            controllertriggerR = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            controllertriggerL = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            if ((controllertriggerR > 0) || GetkeyF)
            {
                Quaternion newspeed;
                Vector3 tempdir;
                Vector3 PlayerVel = localPlayer.GetVelocity();
                if (GetkeyF)
                {
                    newspeed = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    tempdir = newspeed * (Vector3.forward);
                }
                else
                {
                    newspeed = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0);
                    tempdir = newspeed * Vector3.forward;
                }
                //get backwards amount
                float BackThrustAmount = Mathf.Clamp(((Vector3.Dot(PlayerVel, tempdir.normalized)) * BackThrustStrength) * -1, 0, 99999);

                tempdir = (tempdir * flystr) * Mathf.Max(controllertriggerR, Ff);
                float FinalCheatThrust = Mathf.Max(1, (BackThrustAmount * Mathf.Max(controllertriggerR, Ff)));
                localPlayer.SetVelocity(PlayerVel + tempdir * FinalCheatThrust);
            }
            if ((controllertriggerL > 0) || GetkeySpace)
            {
                Vector3 tempdir = ((Vector3.up * flystr) * Mathf.Max(controllertriggerL, GetkeySpace ? 1 : 0));
                localPlayer.SetVelocity(localPlayer.GetVelocity() + tempdir);
            }
        }
    }
}