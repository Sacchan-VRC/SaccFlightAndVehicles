using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EXT_HandTurret : UdonSharpBehaviour
    {
        [Tooltip("Transform that dictates the up direction of the turret")]
        public Transform TurretTransform;
        [Tooltip("Transform that rotates the gun")]
        public Transform Gun;
        [Tooltip("OPTIONAL: Use a separate transform for the pitch rotation")]
        public Transform GunPitch;
        public bool UseLeftHand;
        [Tooltip("Just use the direction that hand is pointing to aim?")]
        public bool Aim_HandDirection;
        [Tooltip("Use look direction for aiming, even in VR")]
        public bool Aim_HeadDirectionVR;
        [Tooltip("Limit the angle the gun can turn to?")]
        public bool LimitTurnAngle;
        public float AngleLimitLeft = 45f;
        public float AngleLimitRight = 45f;
        public float AngleLimitUp = 45f;
        public float AngleLimitDown = 45f;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
        private bool InVR;
        private VRCPlayerApi localPlayer;
        private bool Piloting;
        private float TimeSinceSerialization;
        private Quaternion NonOwnerGunAngleSlerper;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            InVR = EntityControl.InVR;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            InVR = EntityControl.InVR;
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            NonOwnerGunAngleSlerper = Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0));
        }
        public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit() { gameObject.SetActive(false); }
        public void SFEXT_G_Explode()
        {
            Gun.localRotation = Quaternion.identity;
            if (GunPitch) GunPitch.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
            gameObject.SetActive(false);
        }
        public void SFEXT_G_RespawnButton()
        {
            Gun.localRotation = Quaternion.identity;
            if (GunPitch) GunPitch.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (Piloting)
            {
                if (InVR && !Aim_HeadDirectionVR)
                {
                    if (Aim_HandDirection)
                    {
                        if (UseLeftHand)
                        { Gun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(0, 60, 0); }
                        else
                        { Gun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0); }
                    }
                    else
                    {
                        Vector3 lookpoint;
                        if (UseLeftHand)
                        {
                            lookpoint = ((Gun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position) * 500) + Gun.position;
                        }
                        else
                        {
                            lookpoint = ((Gun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position) * 500) + Gun.position;
                        }
                        Gun.LookAt(lookpoint, TurretTransform.up);
                    }
                }
                else
                {
                    Gun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                }
                if (LimitTurnAngle)
                {
                    float AngleHor = Gun.localEulerAngles.y;
                    if (AngleHor > 180) { AngleHor -= 360; }
                    float AngleVert = Gun.localEulerAngles.x;
                    if (AngleVert > 180) { AngleVert -= 360; }

                    if (AngleHor > 0)
                    {
                        if (AngleHor > AngleLimitRight)
                        {
                            Gun.localRotation = Quaternion.Euler(new Vector3(AngleVert, AngleLimitRight, 0));
                        }
                    }
                    else
                    {
                        if (AngleHor < -AngleLimitLeft)
                        {
                            Gun.localRotation = Quaternion.Euler(new Vector3(AngleVert, -AngleLimitLeft, 0));
                        }
                    }
                    AngleHor = Gun.localEulerAngles.y;
                    if (AngleHor > 180) { AngleHor -= 360; }

                    if (AngleVert > 0)
                    {
                        if (AngleVert > AngleLimitDown)
                        {
                            Gun.localRotation = Quaternion.Euler(new Vector3(AngleLimitDown, AngleHor, 0));
                        }
                    }
                    else
                    {
                        if (AngleVert < -AngleLimitUp)
                        {
                            Gun.localRotation = Quaternion.Euler(new Vector3(-AngleLimitUp, AngleHor, 0));
                        }
                    }
                }

                //set gun's roll to 0 and do pitch rotator if used
                Vector3 mgrotH = Gun.localEulerAngles;
                if (GunPitch)
                {
                    mgrotH.z = 0;
                    Vector3 mgrotP = mgrotH;
                    mgrotH.x = 0;
                    mgrotP.y = 0;
                    GunPitch.localRotation = Quaternion.Euler(mgrotP);
                }
                else
                { mgrotH.z = 0; }
                Gun.localRotation = Quaternion.Euler(mgrotH);

                if (TimeSinceSerialization > .3f)
                {
                    TimeSinceSerialization = 0;
                    if (GunPitch)
                    {
                        GunRotation.x = GunPitch.rotation.eulerAngles.x;
                        GunRotation.y = GunPitch.rotation.eulerAngles.y;
                    }
                    else
                    {
                        GunRotation.x = Gun.rotation.eulerAngles.x;
                        GunRotation.y = Gun.rotation.eulerAngles.y;
                    }
                    RequestSerialization();
                }
                TimeSinceSerialization += DeltaTime;
            }
            else
            {
                Quaternion mgrot = Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0));
                NonOwnerGunAngleSlerper = Quaternion.Slerp(NonOwnerGunAngleSlerper, mgrot, 4 * DeltaTime);
                Gun.rotation = NonOwnerGunAngleSlerper;
                Vector3 mgrotH = Gun.localEulerAngles;
                if (GunPitch)
                {
                    mgrotH.x = 0;
                    mgrotH.z = 0;
                    Gun.localRotation = Quaternion.Euler(mgrotH);
                    GunPitch.rotation = NonOwnerGunAngleSlerper;
                    Vector3 mgrotP = GunPitch.localEulerAngles;
                    mgrotP.y = 0;
                    mgrotP.z = 0;
                    GunPitch.localRotation = Quaternion.Euler(mgrotP);
                }
                else
                {
                    mgrotH.z = 0;
                    Gun.localRotation = Quaternion.Euler(mgrotH);
                }
            }
        }
    }
}