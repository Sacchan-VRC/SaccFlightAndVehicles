
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EXTP_Turret : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Transform to base your controls on, should be facing the same direction as the seat. If left empty it will be set to the Horizontal Rotator.")]
        public Transform ControlsRoot;
        public Transform TurretRotatorHor;
        public Transform TurretRotatorVert;
        public float TurnSpeedMultiX = 6;
        public float TurnSpeedMultiY = 6;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        [Tooltip("Lerp rotational inputs by this amount when used in desktop mode so the aim isn't too twitchy")]
        public float TurningResponseDesktop = 2f;
        [Tooltip("Rotation slowdown per frame")]
        [Range(0, 1)]
        public float TurnFriction = .04f;
        [Tooltip("Angle above the horizon that this gun can look")]
        public float UpAngleMax = 89;
        [Tooltip("Angle below the horizon that this gun can look")]
        public float DownAngleMax = 0;
        [Tooltip("Angle that this gun can look to the left and right, set to 180 to freely spin")]
        public float SideAngleMax = 180;
        [Tooltip("In seconds")]
        [Range(0.05f, 1f)]
        public float updateInterval = 0.25f;
        public GameObject Projectile;
        public AudioSource FireSound;
        public Camera ViewCamera;
        public GameObject ViewCameraScreen;
        public Transform AmmoBar;
        public int Ammo = 160;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        [Tooltip("Minimum delay between firing")]
        public float FireDelay = 0f;
        [Tooltip("Delay between firing when holding the trigger")]
        public float FireHoldDelay = 0.5f;
        public Transform[] FirePoints;
        public bool SendAnimTrigger = false;
        public Animator TurretAnimator;
        public string AnimTriggerName = "TurretFire";
        [Tooltip("Fired projectiles will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private float LastFireTime = 0f;
        private int FullAmmo;
        private float FullAmmoDivider;
        private float StartHorTurnSpeed;
        private float StartVertTurnSpeed;
        private float InputXKeyb;
        private float InputYKeyb;
        private float RotationSpeedX = 0f;
        private float RotationSpeedY = 0f;
        private Vector3 AmmoBarScaleStart;
        private float reloadspeed;
        private bool InEditor = true;
        private bool RGripLastFrame;
        private bool InVR;
        private VRCPlayerApi localPlayer;

        private int StartupTimeMS = 0;
        private int O_LastUpdateTime;
        private int L_UpdateTime;
        private int L_LastUpdateTime;
        private float LastPing;
        private float Ping;
        private float nextUpdateTime = 0;
        private double StartupTime;
        private Vector2 LastGunRotationSpeed;
        private Vector2 GunRotationSpeed;
        private Vector2 O_LastGunRotation2;
        private Vector2 O_LastGunRotation;
        private int O_LastUpdateTime2;
        private float SmoothingTimeDivider;
        private bool ClampHor = false;
        private bool Occupied;
        private bool TriggerLastFrame = true;
        private bool Manning;
        private int NumChildrenStart;
        Quaternion ControlsRotLastFrame;
        Quaternion JoystickZeroPoint;
        [System.NonSerializedAttribute] public bool IsOwner;//required by the bomb script, not actually related to being the owner of the object
        [UdonSynced(UdonSyncMode.None)] private Vector2 O_GunRotation;
        [UdonSynced(UdonSyncMode.None)] private int O_UpdateTime = 0;
        public void SFEXTP_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            if (!ControlsRoot) { ControlsRoot = TurretRotatorHor; }

            nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
            SmoothingTimeDivider = 1f / updateInterval;
            StartupTimeMS = Networking.GetServerTimeInMilliseconds();
            FullAmmo = Ammo;
            FullAmmoDivider = 1f / (Ammo > 0 ? Ammo : 10000000);
            if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; }
            reloadspeed = FullAmmo / FullReloadTimeSec;
            if (SideAngleMax < 180) { ClampHor = true; }

            NumChildrenStart = transform.childCount;
            if (Projectile)
            {
                int NumToInstantiate = Mathf.Min(FullAmmo, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(Projectile);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXTP_O_UserEnter()
        {
            TriggerLastFrame = true;
            LastFireTime = Time.time;
            IsOwner = true;
            Manning = true;
            if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Active));
            if (AmmoBar) { AmmoBar.gameObject.SetActive(true); }
            if (ViewCamera) { ViewCamera.gameObject.SetActive(true); }
            if (ViewCameraScreen) { ViewCameraScreen.gameObject.SetActive(true); }
        }
        public void SFEXTP_O_UserExit()
        {
            IsOwner = false;
            SendCustomEventDelayedFrames(nameof(ManningFalse), 1);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_NotActive));
            if (AmmoBar) { AmmoBar.gameObject.SetActive(false); }
            if (ViewCamera) { ViewCamera.gameObject.SetActive(false); }
            if (ViewCameraScreen) { ViewCameraScreen.gameObject.SetActive(false); }
            RotationSpeedX = 0;
            RotationSpeedY = 0;
        }
        public void Set_Active()
        {
            gameObject.SetActive(true);
            if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
        }
        public void Set_NotActive() { gameObject.SetActive(false); }
        public void ManningFalse()
        { Manning = false; }//if this is in SFEXTP_O_UserExit rather than here update runs for one frame with it false before it's disabled    
        public void FireGun()
        {
            int fp = FirePoints.Length;
            if (Ammo > 0) { Ammo--; }
            for (int x = 0; x < fp; x++)
            {
                GameObject proj;
                if (transform.childCount - NumChildrenStart > 0)
                { proj = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { proj = InstantiateWeapon(); }
                if (WorldParent) { proj.transform.SetParent(WorldParent); }
                else { proj.transform.SetParent(null); }
                proj.transform.SetPositionAndRotation(FirePoints[x].position, FirePoints[x].rotation);
                proj.SetActive(true);
                proj.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
            }
            FireSound.pitch = Random.Range(.94f, 1.08f);
            FireSound.PlayOneShot(FireSound.clip);
            if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
            if (SendAnimTrigger) { TurretAnimator.SetTrigger(AnimTriggerName); }
        }
        public void SFEXTP_G_ReSupply()
        {
            if (Ammo != FullAmmo) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            Ammo = (int)Mathf.Min(Ammo + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAmmo);
            if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
        }
        public void SFEXTP_G_RespawnButton()
        {
            Ammo = FullAmmo;
            if (AmmoBar) { AmmoBar.localScale = AmmoBarScaleStart; }
            TurretRotatorHor.localRotation = Quaternion.identity;
            TurretRotatorVert.localRotation = Quaternion.identity;
        }
        public void SFEXTP_G_Explode()
        {
            Ammo = FullAmmo;
            if (AmmoBar) { AmmoBar.localScale = AmmoBarScaleStart; }
            TurretRotatorHor.localRotation = Quaternion.identity;
            TurretRotatorVert.localRotation = Quaternion.identity;
        }
        private void Update()
        {
            if (Manning)
            {
                //GUN
                float Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                {
                    if (!TriggerLastFrame)
                    {
                        if (Ammo > 0 && ((Time.time - LastFireTime) > FireDelay))
                        {
                            LastFireTime = Time.time;
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireGun));
                        }
                    }
                    else if (Ammo > 0 && ((Time.time - LastFireTime) > FireHoldDelay))
                    {//launch every FireHoldDelay
                        LastFireTime = Time.time;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireGun));
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }



                //ROTATION
                float DeltaTime = Time.smoothDeltaTime;
                //get inputs
                int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                int Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                float RGrip = 0;
                float RTrigger = 0;
                float LTrigger = 0;
                if (!InEditor)
                {
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                }
                //virtual joystick
                Vector2 VRPitchYawInput = Vector2.zero;
                if (InVR)
                {
                    if (RGrip > 0.75)
                    {
                        Quaternion RotDif = ControlsRoot.rotation * Quaternion.Inverse(ControlsRotLastFrame);//difference in vehicle's rotation since last frame
                        JoystickZeroPoint = RotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!RGripLastFrame)//first frame you gripped joystick
                        {
                            RotDif = Quaternion.identity;
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                        }
                        RGripLastFrame = true;
                        //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                        Quaternion JoystickDifference = (Quaternion.Inverse(ControlsRoot.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint) * ControlsRoot.rotation;
                        //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                        Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                        //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and multiply for desired deflection
                        //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                        VRPitchYawInput.x = ((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.y, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                        VRPitchYawInput.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                    }
                    else
                    {
                        VRPitchYawInput = Vector3.zero;
                        RGripLastFrame = false;
                    }
                    ControlsRotLastFrame = ControlsRoot.rotation;
                }
                int InX = (Wf + Sf);
                int InY = (Af + Df);
                if (InX > 0 && InputXKeyb < 0 || InX < 0 && InputXKeyb > 0) InputXKeyb = 0;
                if (InY > 0 && InputYKeyb < 0 || InY < 0 && InputYKeyb > 0) InputYKeyb = 0;
                InputXKeyb = Mathf.Lerp(InputXKeyb, InX, Mathf.Abs(InX) > 0 ? TurningResponseDesktop * DeltaTime : 1);
                InputYKeyb = Mathf.Lerp(InputYKeyb, InY, Mathf.Abs(InY) > 0 ? TurningResponseDesktop * DeltaTime : 1);

                float InputX = Mathf.Clamp((VRPitchYawInput.x + InputXKeyb), -1, 1);
                float InputY = Mathf.Clamp((VRPitchYawInput.y + InputYKeyb), -1, 1);

                InputX *= TurnSpeedMultiX;
                InputY *= TurnSpeedMultiY;

                RotationSpeedX += -(RotationSpeedX * TurnFriction) + (InputX);
                RotationSpeedY += -(RotationSpeedY * TurnFriction) + (InputY);

                //rotate turret
                Vector3 rothor = TurretRotatorHor.localRotation.eulerAngles;
                Vector3 rotvert = TurretRotatorVert.localRotation.eulerAngles;

                float NewX = rotvert.x;
                NewX += RotationSpeedX * DeltaTime;
                if (NewX > 180) { NewX -= 360; }
                if (NewX > DownAngleMax || NewX < -UpAngleMax) RotationSpeedX = 0;
                NewX = Mathf.Clamp(NewX, -UpAngleMax, DownAngleMax);//limit angles

                float NewY = rothor.y;
                NewY += RotationSpeedY * DeltaTime;
                if (NewY > 180) { NewY -= 360; }
                if (NewY > SideAngleMax || NewY < -SideAngleMax) RotationSpeedY = 0;
                NewY = Mathf.Clamp(NewY, -SideAngleMax, SideAngleMax);//limit angles

                TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, NewY, 0));
                TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(NewX, 0, 0));

                if (Time.time > nextUpdateTime)
                {
                    O_UpdateTime = Networking.GetServerTimeInMilliseconds();
                    O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.localEulerAngles.y);
                    RequestSerialization();
                    nextUpdateTime = Time.time + updateInterval;
                }
            }
            else
            {
                float TimeSinceUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_UpdateTime) * .001f);
                Vector2 prediction = (GunRotationSpeed * (Ping + TimeSinceUpdate));
                //clamp angle in a way that will never cause an overshoot to clip to the other side
                if (ClampHor)
                {
                    float maxturn;
                    if (O_GunRotation.y < 180)//looking right
                    {
                        if (prediction.y > 0)//moving right
                        {
                            maxturn = SideAngleMax - O_GunRotation.y;
                            if (prediction.y > maxturn)
                            { prediction.y = maxturn; }
                        }
                        else//moving left
                        {
                            maxturn = O_GunRotation.y + SideAngleMax;
                            if (-prediction.y > maxturn)
                            { prediction.y = -maxturn; }
                        }
                    }
                    else//looking left
                    {
                        if (prediction.y > 0)//moving right
                        {
                            maxturn = 360 - O_GunRotation.y + SideAngleMax;
                            if (prediction.y > maxturn)
                            { prediction.y = maxturn; }
                        }
                        else//moving left
                        {
                            maxturn = SideAngleMax - (360 - O_GunRotation.y);
                            if (-prediction.y > maxturn)
                            { prediction.y = -maxturn; }
                        }
                    }
                }
                Vector2 PredictedRotation = O_GunRotation + prediction;
                PredictedRotation.x = Mathf.Clamp(PredictedRotation.x, -UpAngleMax, DownAngleMax);
                //previous imperfect clamp
                /*             if (PredictedRotation.y > SideAngleMax && PredictedRotation.y < 360 - SideAngleMax)
                            {
                                if (O_GunRotation.y > 180)
                                { PredictedRotation.y = 360 - SideAngleMax; }
                                else
                                { PredictedRotation.y = SideAngleMax; }
                            } */
                Vector3 PredictedRotation_3 = new Vector3(PredictedRotation.x, PredictedRotation.y, 0);

                if (TimeSinceUpdate < updateInterval)
                {
                    float TimeSincePreviousUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_LastUpdateTime) * .001f);
                    Vector2 oldprediction = (LastGunRotationSpeed * (LastPing + TimeSincePreviousUpdate));
                    //clamp angle in a way that will never cause an overshoot to clip to the other side
                    if (ClampHor)
                    {
                        float maxturn;
                        if (O_LastGunRotation2.y < 180)//looking right
                        {
                            if (oldprediction.y > 0)//moving right
                            {
                                maxturn = SideAngleMax - O_LastGunRotation2.y;
                                if (oldprediction.y > maxturn)
                                { oldprediction.y = maxturn; }
                            }
                            else//moving left
                            {
                                maxturn = O_LastGunRotation2.y + SideAngleMax;
                                if (-oldprediction.y > maxturn)
                                { oldprediction.y = -maxturn; }
                            }
                        }
                        else//looking left
                        {
                            if (oldprediction.y > 0)//moving right
                            {
                                maxturn = 360 - O_LastGunRotation2.y + SideAngleMax;
                                if (oldprediction.y > maxturn)
                                { oldprediction.y = maxturn; }
                            }
                            else//moving left
                            {
                                maxturn = SideAngleMax - (360 - O_LastGunRotation2.y);
                                if (-oldprediction.y > maxturn)
                                { oldprediction.y = -maxturn; }
                            }
                        }
                    }
                    Vector2 OldPredictedRotation = O_LastGunRotation2 + oldprediction;
                    OldPredictedRotation.x = Mathf.Clamp(OldPredictedRotation.x, -UpAngleMax, DownAngleMax);
                    Vector3 OldPredictedRotation_3 = new Vector3(OldPredictedRotation.x, OldPredictedRotation.y, 0);

                    Vector3 TargetRot = Vector3.Lerp(OldPredictedRotation_3, PredictedRotation_3, TimeSinceUpdate * SmoothingTimeDivider);
                    TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, TargetRot.y, 0));
                    TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(TargetRot.x, 0, 0));
                }
                else
                {
                    TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, PredictedRotation_3.y, 0));
                    TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(PredictedRotation_3.x, 0, 0));
                }
            }
        }
        public void SFEXTP_O_PlayerJoined()
        {
            if (localPlayer.IsOwner(gameObject) && Manning)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Active));
            }
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {//when we take ownership because someone timed out, we don't want it left active
            if (player.isLocal && gameObject.activeSelf && !Manning)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_NotActive));
            }
        }
        public override void OnDeserialization()
        {
            if (O_UpdateTime != O_LastUpdateTime)//only do anything if OnDeserialization was for this script
            {
                if (O_GunRotation.x > 180) { O_GunRotation.x -= 360; }
                LastPing = Ping;
                L_LastUpdateTime = L_UpdateTime;
                float updatedelta = (O_UpdateTime - O_LastUpdateTime) * .001f;
                float speednormalizer = 1 / updatedelta;

                L_UpdateTime = Networking.GetServerTimeInMilliseconds();
                Ping = (L_UpdateTime - O_UpdateTime) * .001f;
                LastGunRotationSpeed = GunRotationSpeed;

                //check if going from rotation 0->360 and fix values for interpolation
                if (Mathf.Abs(O_GunRotation.y - O_LastGunRotation.y) > 180)
                {
                    if (O_GunRotation.y > O_LastGunRotation.y)
                    {
                        O_LastGunRotation.y += 360;
                    }
                    else
                    {
                        O_LastGunRotation.y -= 360;
                    }
                }
                GunRotationSpeed = (O_GunRotation - O_LastGunRotation) * speednormalizer;
                O_LastGunRotation2 = O_LastGunRotation;
                O_LastGunRotation = O_GunRotation;
                O_LastUpdateTime = O_UpdateTime;
            }
        }
    }
}