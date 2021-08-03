
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNCP_Minigun : UdonSharpBehaviour
{
    [SerializeField] private Transform VehicleTransform;
    [SerializeField] private Transform Minigun;
    [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
    private bool InVR;
    private VRCPlayerApi localPlayer;
    private bool UseLeftTrigger;
    private bool active;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_ECStart()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            InVR = localPlayer.IsUserInVR();
        }
    }
    public void DFUNC_Selected()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Activate");
        active = true;
    }
    public void DFUNC_Deselected()
    {
        if (active)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Deactivate");
            active = false;
        }
    }
    public void SFEXTP_O_UserExit()
    {
        DFUNC_Deselected();
    }
    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (active)
        {
            if (InVR)
            {
                Vector3 lookpoint = (Minigun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position * 500) + Minigun.position;
                Minigun.LookAt(lookpoint, VehicleTransform.up);
            }
            else
            {
                Minigun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            }
        }
    }
}
