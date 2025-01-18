
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccAAGunController : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [Tooltip("The part of the AAGun that aims")]
        public Transform Rotator;
        public SAAG_HUDController HUDControl;
        [Tooltip("Missile object to be duplicated and enabled when a missile is fired")]
        public GameObject AAM;
        [Range(0, 2)]
        [Tooltip("0 = Radar, 1 = Heat, 2 = Other. Controls what variable is added to in SaccAirVehicle to count incoming missiles, AND which variable to check for reduced tracking, (MissilesIncomingHeat NumActiveFlares, MissilesIncomingRadar NumActiveChaff, MissilesIncomingOther NumActiveOtherCM)")]
        public int MissileType = 1;
        [Tooltip("Audio source that plays when rotating")]
        public AudioSource RotatingSound;
        public float RotatingSoundMulti = .02f;
        [Tooltip("Sound that plays when targeting an enemy")]
        public AudioSource AAMTargeting;
        [Tooltip("Sound that plays when locked onto a target")]
        public AudioSource AAMTargetLock;
        [Tooltip("Sound that plays when a target is hit")]
        public AudioSource DamageFeedBack;
        [Tooltip("Joystick object that moves around in to show rotation inputs")]
        public Transform JoyStick;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        [Tooltip("Joystick controlled by left hand?")]
        public bool SwitchHandsJoyThrottle = false;
        [Tooltip("When destroyed, will reappear after this many seconds")]
        public float RespawnDelay = 20;
        [Tooltip("Vehicle is un-destroyable for this long after spawning")]
        public float InvincibleAfterSpawn = 1;
        public float Health = 100f;
        [Tooltip("Rotation strength")]
        public float TurnSpeedMulti = 6;
        [Tooltip("Rotation slowdown per frame")]
        public float TurnFriction = 4f;
        [Tooltip("Angle above the horizon that this gun can look")]
        public float UpAngleMax = 89;
        [Tooltip("Angle below the horizon that this gun can look")]
        public float DownAngleMax = 35;
        [Tooltip("Lerp rotational inputs by this amount when used in desktop mode so the aim isn't too twitchy")]
        public float TurningResponseDesktop = 2f;
        [Tooltip("HP repairs every x seconds")]
        public float HPRepairDelay = 5f;
        [Tooltip("HP will start repairing this long after being hit")]
        public float HPRepairHitTimer = 10f;
        [Tooltip("HP repairs by this amount every HPRepairDelay seconds")]
        public float HPRepairAmount = 5f;
        public float MissileReloadTime = 10;
        [Tooltip("How long gun can fire for before running out of ammo in seconds")]
        public float MGAmmoSeconds = 4;
        [Tooltip("How fast ammo reloads")]
        public float MGReloadSpeed = 1;
        [Tooltip("How long after stopping firing before ammo starts recharging")]
        public float MGReloadDelay = 2;
        public int NumAAM = 4;
        public float AAMMaxTargetDistance = 6000;
        [Tooltip("If target is within this angle of the direction the gun is aiming, it is lockable")]
        public float AAMLockAngle = 20;
        [Tooltip("AAM takes this long to lock before it can fire (seconds)")]
        public float AAMLockTime = 1.5f;
        [Tooltip("Heatseekers only: How much faster is locking if the target has afterburner on? (AAMLockTime / value)")]
        public float LockTimeABDivide = 2f;
        [Tooltip("Heatseekers only: If target's engine throttle is 0%, what is the minimum number to divide lock time by, to prevent infinite lock time. (AAMLockTime / value)")]
        public float LockTimeMinDivide = .2f;
        [Tooltip("Minimum time between missile launches")]
        public float AAMLaunchDelay = 0f;
        [Tooltip("Point missile is launched from, flips on local X each time fired")]
        public Transform AAMLaunchPoint;
        [Tooltip("Allow locking on target with no missiles left. Enable if creating FOX-1/3 missiles, otherwise your last missile will be unusable.")]
        public bool AllowNoAmmoLock = false;
        [Tooltip("Make it only possible to lock if the angle you are looking at the back of the enemy plane is less than HighAspectPreventLock (for heatseekers)")]
        public bool HighAspectPreventLock;
        [Tooltip("Angle beyond which aspect is too high to lock")]
        public float HighAspectAngle = 85;
        [Tooltip("Require re-lock after firing?")]
        public bool LoseLockWhenFired = false;
        [Tooltip("Send the missile warning alarm to aircraft?")]
        public bool SendLockWarning = true;
        [Tooltip("Tick this to disable target tracking, prediction, and missiles (WW2 flak?)")]
        public bool DisableTargeting;
        [Tooltip("Ignore targets that don't have a SaccAirVehicle script?")]
        public bool OnlyTargetVehicles;
        [Tooltip("Multiplies how much damage is taken from bullets")]
        public float BulletDamageTaken = 10f;
        private float HighAspectPreventLockAngleDot;
        private bool TriggerLastFrame;
        [Tooltip("Layers to check raycast hits for (in place of OnboardVehicleLayer and OutsideVehicleLayer) (OnboardVehicleLayer is required for an AAGun you own to target yourself)")]
        [SerializeField] private int[] TargetLayers = { 17, 31 };
        public bool AI_GUN;
        [SerializeField] private float AI_GUN_TurnStrength = 1f;
        [SerializeField] private Vector2 AI_GUN_BurstLength = new Vector2(1.5f, 4.0f);
        [SerializeField] private Vector2 AI_GUN_BurstPauseLength = new Vector2(0.4f, 1.4f);
        [SerializeField] private float AI_GUN_MissileInterval = 6f;
        [System.NonSerializedAttribute] public bool AI_GUN_RUNNINGLOCAL;
        private bool AI_GUN_WantsToFire;
        private bool AI_GUN_NOGUN;
        private bool AI_GUN_FIRING;
        public GameObject PitBullIndicator;
        public bool PredictDamage = true;
        private float PredictedHealth;
        private float LastHitTime;
        private float MGAmmoRecharge = 0;
        [System.NonSerializedAttribute] public float MGAmmoFull = 4;
        private float FullMGDivider;
        [System.NonSerializedAttribute] public Animator AAGunAnimator;
        [System.NonSerializedAttribute] public float FullHealth;
        [System.NonSerializedAttribute] public bool Manning = false;//like Piloting in the plane
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        private float RotationSpeedX = 0f;
        private float RotationSpeedY = 0f;
        private Quaternion StartRot;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool IsOwner = false;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
        [HideInInspector] public GameObject[] AAMTargets;
        [HideInInspector] public int NumAAMTargets = 0;
        private int AAMTargetChecker = 0;
        [System.NonSerializedAttribute, UdonSynced] public bool AAMHasTarget;
        private float AAMTargetedTime = 2f;
        [System.NonSerializedAttribute] public bool AAMLocked = false;
        [System.NonSerializedAttribute] public float AAMLockTimer = 0;
        private float AAMLastFiredTime;
        [System.NonSerializedAttribute] public Vector3 AAMCurrentTargetDirection;
        [System.NonSerializedAttribute] public SaccAirVehicle AAMCurrentTargetSAVControl;
        private float AAMTargetObscuredDelay;
        [System.NonSerializedAttribute] public bool InVR;
        Quaternion AAGunRotLastFrame;
        Quaternion JoystickZeroPoint;
        [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
        private float FullAAMsDivider;
        private float FullHealthDivider;
        private float RotateSoundVol;
        private bool LTriggerLastFrame;
        private int NumChildrenStart;
        [System.NonSerializedAttribute] public bool DoAAMTargeting = false;
        [System.NonSerializedAttribute] public int FullAAMs;
        [System.NonSerializedAttribute] public float AAMReloadTimer;
        [System.NonSerializedAttribute] public float HealthUpTimer;
        [System.NonSerializedAttribute] public float HPRepairTimer;
        [System.NonSerializedAttribute] private float InputXKeyb;
        [System.NonSerializedAttribute] private float InputYKeyb;
        [System.NonSerializedAttribute] public float LastHealthUpdate = 0;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        private bool Occupied;
        [UdonSynced, FieldChangeCallback(nameof(Firing))] private bool _firing;
        public bool Firing
        {
            set
            {
                if (!EntityControl._dead && (Occupied || AI_GUN))
                {
                    _firing = value;
                    AAGunAnimator.SetBool("firing", value);
                }
                else
                {
                    _firing = false;
                    AAGunAnimator.SetBool("firing", false);
                }
            }
            get => _firing;
        }
        [UdonSynced] private bool AAMFireNow;
        [UdonSynced] private bool SendTargeted;
        private float SendTargeted_Time;
        const float SENDTARGETED_INTERVAL = 1;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) { InEditor = true; Manning = true; IsOwner = true; }
            else
            {
                InEditor = false;
                InVR = EntityControl.InVR;
                IsOwner = EntityControl.IsOwner;
            }
            CenterOfMass = EntityControl.CenterOfMass;

            AAGunAnimator = EntityControl.GetComponent<Animator>();
            FullHealth = Health;
            FullHealthDivider = 1f / (Health > 0 ? Health : 10000000);
            StartRot = Rotator.localRotation;
            if (RotatingSound) { RotateSoundVol = RotatingSound.volume; }

            FullAAMs = NumAAM;
            FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
            MGAmmoFull = MGAmmoSeconds;
            FullMGDivider = 1f / (MGAmmoFull > 0 ? MGAmmoFull : 10000000);
            HighAspectPreventLockAngleDot = Mathf.Cos(HighAspectAngle * Mathf.Deg2Rad);
            if (LockTimeABDivide <= 0)
            { LockTimeABDivide = 0.0001f; }
            if (LockTimeMinDivide <= 0)
            { LockTimeMinDivide = 0.0001f; }

            AAMTargets = EntityControl.AAMTargets;
            NumAAMTargets = AAMTargets.Length;
            if (NumAAMTargets != 0 && !DisableTargeting) { DoAAMTargeting = true; }
            gameObject.SetActive(true);

            HUDControl.RemoteInit();
            if (MGAmmoFull <= 0) AI_GUN_NOGUN = true;
            if (IsOwner && AI_GUN) { AI_GUN_Enter(); }

            NumChildrenStart = transform.childCount;
            int NumToInstantiate = Mathf.Min(FullAAMs, 10);
            for (int i = 0; i < NumToInstantiate; i++)
            {
                InstantiateWeapon();
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Instantiate(AAM);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        void LateUpdate()
        {
            if (IsOwner)
            {
                if (Manning)
                {
                    float DeltaTime = Time.smoothDeltaTime;
                    //get inputs
                    int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                    int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                    int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                    int Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                    float RGrip = 0;
                    float LTrigger = 0;
                    if (!InEditor)
                    {
                        LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                        RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                    }
                    //virtual joystick
                    Vector2 VRPitchYawInput = Vector2.zero;
                    if (InVR)
                    {
                        if (RGrip > 0.75)
                        {
                            Quaternion RotDif = Rotator.rotation * Quaternion.Inverse(AAGunRotLastFrame);//difference in vehicle's rotation since last frame
                            JoystickZeroPoint = RotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                            if (!JoystickGripLastFrame)//first frame you gripped joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickGrabbed");
                                RotDif = Quaternion.identity;
                                JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                            }
                            JoystickGripLastFrame = true;
                            //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                            Quaternion JoystickDifference = (Quaternion.Inverse(Rotator.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint) * Rotator.rotation;
                            //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                            Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                            //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and multiply for desired deflection
                            //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                            VRPitchYawInput.x = ((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.y, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                            VRPitchYawInput.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                        }
                        else
                        {
                            if (JoystickGripLastFrame)//first frame you let go of joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped");
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                            }
                            VRPitchYawInput = Vector3.zero;
                            JoystickGripLastFrame = false;
                        }
                        AAGunRotLastFrame = Rotator.rotation;
                    }
                    int InX = (Wf + Sf);
                    int InY = (Af + Df);
                    if (InX > 0 && InputXKeyb < 0 || InX < 0 && InputXKeyb > 0) InputXKeyb = 0;
                    if (InY > 0 && InputYKeyb < 0 || InY < 0 && InputYKeyb > 0) InputYKeyb = 0;
                    InputXKeyb = Mathf.Lerp(InputXKeyb, InX, Mathf.Abs(InX) > 0 ? TurningResponseDesktop * DeltaTime : 1);
                    InputYKeyb = Mathf.Lerp(InputYKeyb, InY, Mathf.Abs(InY) > 0 ? TurningResponseDesktop * DeltaTime : 1);

                    float InputX = Mathf.Clamp((VRPitchYawInput.x + InputXKeyb), -1, 1);
                    float InputY = Mathf.Clamp((VRPitchYawInput.y + InputYKeyb), -1, 1);
                    //joystick model movement
                    if (JoyStick)
                    {
                        JoyStick.localRotation = Quaternion.Euler(new Vector3(InputX * 25f, InputY * 25f, 0));
                    }
                    InputX *= TurnSpeedMulti;
                    InputY *= TurnSpeedMulti;

                    RotateGun(InputX, InputY);
                    GunFireCheck();

                    bool lockedLast = AAMLocked;
                    if (DoAAMTargeting)
                    {
                        if (MissileType == 1)//heatseekers check engine output of target
                        {
                            if (AAMCurrentTargetSAVControl ?//if target is SaccAirVehicle, adjust lock time based on throttle status 
                            AAMLockTimer > AAMLockTime /
                            (AAMCurrentTargetSAVControl.AfterburnerOn ?
                                        LockTimeABDivide
                                        :
                                        Mathf.Clamp(AAMCurrentTargetSAVControl.EngineOutput / AAMCurrentTargetSAVControl.ThrottleAfterburnerPoint, LockTimeMinDivide, 1))
                            : AAMLockTimer > AAMLockTime && AAMHasTarget)//target is not a SaccAirVehicle
                            { AAMLocked = true; }
                            else { AAMLocked = false; }
                        }
                        else
                        {
                            if (AAMLockTimer > AAMLockTime && AAMHasTarget) { AAMLocked = true; }
                            else { AAMLocked = false; }
                        }
                        if (lockedLast != AAMLocked) { RequestSerialization(); }

                        //firing AAM
                        if (LTrigger > 0.75 || (Input.GetKey(KeyCode.C)))
                        {
                            if (!LTriggerLastFrame)
                            {
                                if (AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                                {
                                    LaunchAAM_Owner();
                                }
                            }
                            LTriggerLastFrame = true;
                        }
                        else LTriggerLastFrame = false;
                    }

                    AAMReplenishment();
                    HPReplenishment();

                    //Sounds
                    if (!AAMLocked && AAMLockTimer > 0)
                    {
                        if (AAMTargeting && (NumAAM > 0 || AllowNoAmmoLock)) { AAMTargeting.gameObject.SetActive(true); }
                        if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                    }
                    else if (AAMLocked)
                    {
                        if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                        if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(true); }
                    }
                    else
                    {
                        if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                        if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                    }
                    if (RotatingSound)
                    {
                        float turnvol = new Vector2(RotationSpeedX, RotationSpeedY).magnitude * RotatingSoundMulti;
                        RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                        RotatingSound.pitch = turnvol;
                    }
                }
                else if (AI_GUN_RUNNINGLOCAL && !EntityControl.dead)
                {
                    AAMReplenishment();
                    HPReplenishment();
                    AI_GUN_Look();
                    if (Vector3.Angle(Rotator.forward, HUDControl.GUNLeadIndicator.transform.position - Rotator.position) < 10 && AAMHasTarget)
                    {
                        AI_GUN_FIRING = !AI_GUN_NOGUN && AI_GUN_BurstFire();
                        AI_GUN_WantsToFire = true;
                        if (AAMLockTimer > AAMLockTime && AAMHasTarget) AAMLocked = true;
                        else { AAMLocked = false; }
                        if (NumAAM > 0 && AAMLocked && Time.time - AAMLastFiredTime > AI_GUN_MissileInterval)
                        {
                            LaunchAAM_Owner();
                        }
                    }
                    else { AI_GUN_FIRING = false; AI_GUN_WantsToFire = false; }

                    GunFireCheck();
                }
            }
            else if (AI_GUN)
            {
                AI_GUN_Look();
            }
        }
        private void AI_GUN_Look()
        {
            float InputX;
            float InputY;
            if (AAMHasTarget)
            {
                HUDControl.GUNLead();
                //P Controller Y
                Vector3 gunForward = Rotator.forward;
                Vector3 flattenedTargVec = Vector3.ProjectOnPlane(HUDControl.GUNLeadIndicator.transform.position - Rotator.position, Rotator.up);
                float yAngle = Vector3.SignedAngle(gunForward, flattenedTargVec, Rotator.up);
                InputY = Mathf.Clamp(yAngle * AI_GUN_TurnStrength, -1, 1);

                // Rotate the forward vector to the target on Y so that the X doesn't overshoot
                Quaternion forwardX = Rotator.rotation * Quaternion.AngleAxis(yAngle, EntityControl.transform.up);
                gunForward = Quaternion.AngleAxis(yAngle, EntityControl.transform.up) * gunForward;
                Vector3 xRight = forwardX * Vector3.right;

                //P Controller X
                flattenedTargVec = Vector3.ProjectOnPlane(HUDControl.GUNLeadIndicator.transform.position - Rotator.position, xRight);
                InputX = Vector3.SignedAngle(gunForward, flattenedTargVec, xRight);
                InputX = Mathf.Clamp(InputX * AI_GUN_TurnStrength, -1, 1);
            }
            else
            {
                float NewX = Rotator.localRotation.eulerAngles.x;
                if (NewX > 180) { NewX -= 360; }
                InputX = -45 - NewX;
                InputY = 1f;
                InputX = Mathf.Clamp(InputX * AI_GUN_TurnStrength, -1, 1);
                InputY = Mathf.Clamp(InputY * AI_GUN_TurnStrength, -1, 1);
            }
            InputX *= TurnSpeedMulti;
            InputY *= TurnSpeedMulti;
            RotateGun(InputX, InputY);
        }
        private float AI_GUN_LastGunFire;
        private bool AI_GUN_Pausing;
        private float AI_GUN_StartFiringTime;
        private float AI_GUN_THISPAUSELENGTH;
        private float AI_GUN_THISBURSTLENGTH;
        bool AI_GUN_BurstFire()
        {
            if (AI_GUN_Pausing)
            {
                if (Time.time - AI_GUN_LastGunFire > AI_GUN_THISPAUSELENGTH)
                {
                    AI_GUN_Pausing = false;
                    AI_GUN_StartFiringTime = Time.time;
                    AI_GUN_THISBURSTLENGTH = Random.Range(AI_GUN_BurstLength.x, AI_GUN_BurstLength.y);
                    return true;
                }
                else return false;
            }
            else if (!AI_GUN_WantsToFire)
            {
                AI_GUN_StartFiringTime = Time.time;
                AI_GUN_THISBURSTLENGTH = Random.Range(AI_GUN_BurstLength.x, AI_GUN_BurstLength.y);
                return true;
            }
            else
            {
                if (Time.time - AI_GUN_StartFiringTime < AI_GUN_THISBURSTLENGTH)
                {
                    return true;
                }
                else
                {
                    AI_GUN_Pausing = true;
                    AI_GUN_LastGunFire = Time.time;
                    AI_GUN_THISPAUSELENGTH = Random.Range(AI_GUN_BurstPauseLength.x, AI_GUN_BurstPauseLength.y);
                    return false;
                }
            }
        }
        private void AAMReplenishment()
        {
            //reloading AAMs
            if (NumAAM == FullAAMs)
            { AAMReloadTimer = 0; }
            else
            { AAMReloadTimer += Time.deltaTime; }
            if (AAMReloadTimer > MissileReloadTime)
            {
                if (InEditor)
                { ReloadAAM(); }
                else
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReloadAAM)); }
            }
        }
        private void HPReplenishment()
        {
            //HP Repair
            if (Health == FullHealth)
            { HPRepairTimer = 0; }
            else
            { HPRepairTimer += Time.deltaTime; }
            if (HPRepairTimer > HPRepairDelay)
            {
                if (InEditor)
                { HPRepair(); }
                else
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(HPRepair)); }
            }
        }
        void RotateGun(float inputx, float inputy)
        {
            float deltaTime = Time.deltaTime;
            // RotationSpeedX += -(RotationSpeedX * TurnFriction) + (inputx);
            // RotationSpeedY += -(RotationSpeedY * TurnFriction) + (inputy);
            RotationSpeedX = Mathf.Lerp(RotationSpeedX, 0, (1 - Mathf.Pow(0.5f, TurnFriction * deltaTime))) + inputx * deltaTime;
            RotationSpeedY = Mathf.Lerp(RotationSpeedY, 0, (1 - Mathf.Pow(0.5f, TurnFriction * deltaTime))) + inputy * deltaTime;

            //rotate turret
            Vector3 rot = Rotator.localRotation.eulerAngles;
            float NewX = rot.x;
            NewX += RotationSpeedX * deltaTime;
            if (NewX > 180) { NewX -= 360; }
            if (NewX > DownAngleMax || NewX < -UpAngleMax) RotationSpeedX = 0;
            NewX = Mathf.Clamp(NewX, -UpAngleMax, DownAngleMax);//limit angles
            float NewY = rot.y + (RotationSpeedY * deltaTime);
            Rotator.localRotation = Quaternion.Euler(new Vector3(NewX, NewY, 0));
        }
        void GunFireCheck()
        {
            float RTrigger = 0;
            if (!InEditor)
            {
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            }
            //Firing the gun
            if ((RTrigger >= 0.75 || Input.GetKey(KeyCode.Space)) && MGAmmoSeconds > 0 && !AI_GUN || AI_GUN_FIRING)
            {
                if (!_firing)
                {
                    Firing = true;
                    RequestSerialization();
                    if (IsOwner)
                    { EntityControl.SendEventToExtensions("SFEXT_O_GunStartFiring"); }
                }
                MGAmmoSeconds -= Time.deltaTime;
                MGAmmoRecharge = MGAmmoSeconds - MGReloadDelay;
            }
            else//recharge the ammo
            {
                if (_firing)
                {
                    Firing = false;
                    RequestSerialization();
                    if (IsOwner)
                    { EntityControl.SendEventToExtensions("SFEXT_O_GunStopFiring"); }
                }
                MGAmmoRecharge = Mathf.Min(MGAmmoRecharge + (Time.deltaTime * MGReloadSpeed), MGAmmoFull);
                MGAmmoSeconds = Mathf.Max(MGAmmoRecharge, MGAmmoSeconds);
            }
        }
        private void FixedUpdate()
        {
            if (DoAAMTargeting && (Manning || (IsOwner && AI_GUN_RUNNINGLOCAL)))
            {
                AAMFindTargets(AAMLockAngle);
            }
        }
        public void NetworkExplode()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
        }
        public void Explode()//all the things players see happen when the vehicle explodes
        {
            if (EntityControl._dead) { return; }
            if (Manning && !InEditor)
            {
                EntityControl.ExitStation();
            }
            EntityControl.dead = true;
            Firing = false;
            MGAmmoSeconds = MGAmmoFull;
            Health = FullHealth;//turns off low health smoke and stops it from calling Explode() every frame
            NumAAM = FullAAMs;
            AAGunAnimator.SetBool("firing", false);
            AAGunAnimator.SetFloat("AAMs", (float)FullAAMs * FullAAMsDivider);
            AAGunAnimator.SetFloat("health", 1);
            if (IsOwner)
            {
                Rotator.localRotation = StartRot;
            }
            AAGunAnimator.SetTrigger("explode");

            SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
            SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);
            EntityControl.SendEventToExtensions("SFEXT_G_Explode");
        }
        public void ReAppear()
        {
            AAGunAnimator.SetTrigger("reappear");
            EntityControl.dead = false;
            if (IsOwner)
            {
                Health = FullHealth;
            }
            EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
        }
        public void NotDead()
        {
            Health = FullHealth;
            EntityControl.dead = false;
        }
        public void SendBulletHit()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void SFEXT_L_BulletHit()
        {
            if (PredictDamage)
            {
                if (Time.time - LastHitTime > 2)
                {
                    LastHitTime = Time.time;//must be updated before sending explode() for checks in explode event to work
                    PredictedHealth = Health - (BulletDamageTaken * EntityControl.LastHitBulletDamageMulti);
                    if (!EntityControl.dead && PredictedHealth < 0)
                    {
                        NetworkExplode();
                    }
                }
                else
                {
                    LastHitTime = Time.time;
                    PredictedHealth -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    if (!EntityControl.dead && PredictedHealth < 0)
                    {
                        NetworkExplode();
                    }
                }
            }
        }
        public void SFEXT_G_BulletHit()
        {
            if (!EntityControl._dead)
            {
                if (IsOwner)
                {
                    Health -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    if (PredictDamage && Health <= 0)//the attacker calls the explode function in this case
                    {
                        Health = 0.0911f;
                        //if two people attacked us, and neither predicted they killed us but we took enough damage to die, we must still die.
                        SendCustomEventDelayedSeconds(nameof(CheckLaggyKilled), .25f);//give enough time for the explode event to happen if they did predict we died, otherwise do it ourself
                    }
                }
            }
        }
        public void CheckLaggyKilled()
        {
            if (!EntityControl._dead)
            {
                //Check if we still have the amount of health set to not send explode when killed, and if we do send explode
                if (Health == 0.0911f)
                {
                    NetworkExplode();
                }
            }
        }
        bool rayhitIsOnCorrectLayer(RaycastHit target)
        {
            //hitnext.collider && (hitnext.collider.gameObject.layer == 17 || hitnext.collider.gameObject.layer == 31)
            if (!target.collider) return false;
            int targlayer = target.collider.gameObject.layer;
            for (int i = 0; i < TargetLayers.Length; i++)
            {
                if (targlayer == TargetLayers[i])
                    return true;
            }
            return false;
        }
        bool rayhitIsNotOnCorrectLayer(RaycastHit target)
        {
            //(hitcurrent.collider && hitcurrent.collider.gameObject.layer != 17 && hitcurrent.collider.gameObject.layer != 31)
            if (!target.collider) return false;
            int targlayer = target.collider.gameObject.layer;
            for (int i = 0; i < TargetLayers.Length; i++)
            {
                if (targlayer == TargetLayers[i])
                    return false;
            }
            return true;
        }
        private void AAMFindTargets(float Lock_Angle)
        {
            float DeltaTime = Time.deltaTime;
            var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
            Vector3 HudControlPosition = HUDControl.transform.position;
            float AAMCurrentTargetAngle = Vector3.Angle(Rotator.forward, (AAMCurrentTargetPosition - HudControlPosition));

            //check 1 target per frame to see if it's infront of us and worthy of being our current target
            var TargetChecker = AAMTargets[AAMTargetChecker];
            var TargetCheckerTransform = TargetChecker.transform;
            var TargetCheckerParent = TargetCheckerTransform.parent;

            Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
            float NextTargetAngle = Vector3.Angle(Rotator.forward, AAMNextTargetDirection);
            float NextTargetDistance = Vector3.Distance(HudControlPosition, TargetCheckerTransform.position);

            if (TargetChecker.activeInHierarchy)
            {
                SaccAirVehicle NextTargetSAVControl = null;

                if (TargetCheckerParent)
                {
                    NextTargetSAVControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
                }
                //if target SaccAirVehicle is null then it's a dummy target (or hierarchy isn't set up properly)
                if ((!NextTargetSAVControl || (!NextTargetSAVControl.Taxiing && !NextTargetSAVControl.EntityControl._dead)))
                {
                    RaycastHit hitnext;
                    //raycast to check if it's behind something
                    int layermask_Next = 133137;/* Default, Water, Environment, and Walkthrough */
                    if (AI_GUN_RUNNINGLOCAL) layermask_Next += 1 << EntityControl.OnboardVehicleLayer; // add OnBoardVehicleLayer so it can hit you if you're owner of this and in another vehicle
                    bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, Mathf.Infinity, layermask_Next, QueryTriggerInteraction.Ignore);
#if UNITY_EDITOR
                    if (hitnext.collider)
                        Debug.DrawLine(HudControlPosition, hitnext.point, Color.red);
                    else
                        Debug.DrawRay(HudControlPosition, AAMNextTargetDirection, Color.yellow);
#endif
                    /* Debug.Log(string.Concat("LoS ", LineOfSightNext));
                    Debug.Log(string.Concat("RayCastLayer ", hitnext.collider.gameObject.layer == PlaneHitBoxLayer));
                    Debug.Log(string.Concat("InAngle ", NextTargetAngle < Lock_Angle));
                    Debug.Log(string.Concat("BelowMaxDist ", NextTargetDistance < AAMMaxTargetDistance));
                    Debug.Log(string.Concat("LowerAngle ", NextTargetAngle < AAMCurrentTargetAngle));
                    Debug.Log(string.Concat("CurrentTargTaxiing ", !AAMCurrentTargetSAVControlNull && AAMCurrentTargetSAVControl.Taxiing)); */
                    if (LineOfSightNext
                        && rayhitIsOnCorrectLayer(hitnext) //did raycast hit an object on the layer planes are on?
                            && NextTargetAngle < AAMLockAngle
                                && NextTargetAngle < AAMCurrentTargetAngle
                                    && NextTargetDistance < AAMMaxTargetDistance
                                        && (!NextTargetSAVControl ||//null check
                                            ((!HighAspectPreventLock || Vector3.Dot(NextTargetSAVControl.VehicleTransform.forward, AAMNextTargetDirection.normalized) > HighAspectPreventLockAngleDot)
                                            && (MissileType != 1 || NextTargetSAVControl._EngineOn)))
                                        || (AAMCurrentTargetSAVControl &&//null check
                                                                    (AAMCurrentTargetSAVControl.Taxiing ||//switch target if current target is taxiing
                                                                    (MissileType == 1 && !AAMCurrentTargetSAVControl._EngineOn && !AAMCurrentTargetSAVControl.EntityControl.wrecked)))//switch target if heatseeker and current target's engine is off unless target is wrecked(on fire)
                                            || !AAMTargets[AAMTarget].activeInHierarchy//switch target if current target is destroyed
                                            )
                    {
                        if (!LTriggerLastFrame)
                        {
                            //found new target
                            AAMCurrentTargetAngle = NextTargetAngle;
                            AAMTarget = AAMTargetChecker;
                            AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                            AAMCurrentTargetSAVControl = NextTargetSAVControl;
                            AAMLockTimer = 0;
                        }
                    }
                }
            }
            //increase target checker ready for next frame
            AAMTargetChecker++;
            if (AAMTargetChecker == AAMTarget && AAMTarget == NumAAMTargets - 1)
            { AAMTargetChecker = 0; }
            else if (AAMTargetChecker == AAMTarget)
            { AAMTargetChecker++; }
            else if (AAMTargetChecker > NumAAMTargets - 1)
            { AAMTargetChecker = 0; }

            //if target is currently in front of plane, lock onto it
            AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition;
            float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
            //check if target is active, and if it's SaccairVehicle is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
            //raycast to check if it's behind something
            RaycastHit hitcurrent;
            int layermask_Current = 133137;
            if (AI_GUN_RUNNINGLOCAL) layermask_Current += 1 << EntityControl.OnboardVehicleLayer;
            bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, Mathf.Infinity, layermask_Current, QueryTriggerInteraction.Ignore);
#if UNITY_EDITOR
            if (hitcurrent.collider)
                Debug.DrawLine(HudControlPosition, hitcurrent.point, Color.green);
            else
                Debug.DrawRay(HudControlPosition, AAMNextTargetDirection, Color.blue);
#endif
            //used to make lock remain for .25 seconds after target is obscured
            if (!LineOfSightCur || rayhitIsNotOnCorrectLayer(hitcurrent))
            { AAMTargetObscuredDelay += DeltaTime; }
            else
            { AAMTargetObscuredDelay = 0; }

            if (
                (AAMTargetObscuredDelay < .25f)
                    && AAMCurrentTargetDistance < AAMMaxTargetDistance
                        && AAMTargets[AAMTarget].activeInHierarchy
                            && (!AAMCurrentTargetSAVControl ||
                                (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl._dead &&
                                    (MissileType != 1 || (AAMCurrentTargetSAVControl._EngineOn || AAMCurrentTargetSAVControl.EntityControl.wrecked))))//heatseekers cant lock if engine off unless wrecked (on fire)
                                &&
                                    (!HighAspectPreventLock || !AAMCurrentTargetSAVControl || Vector3.Dot(AAMCurrentTargetSAVControl.VehicleTransform.forward, AAMCurrentTargetDirection.normalized) > HighAspectPreventLockAngleDot)
                                    )
            {
                if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < AAMMaxTargetDistance)
                {
                    AAMHasTarget = true;
                    if (AAMCurrentTargetAngle < AAMLockAngle && (NumAAM > 0 || AllowNoAmmoLock))
                    {
                        AAMLockTimer += DeltaTime;
                        if (AAMCurrentTargetSAVControl)
                        {
                            //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                            if (SendLockWarning && Time.time - AAMTargetedTime > SENDTARGETED_INTERVAL)
                            {
                                SendTargeted_Time = Time.time;
                                RequestSerialization();
                                AAMTargetedTime = Time.time;
                            }
                        }
                    }
                    else
                    {
                        AAMTargetedTime = 0;
                        AAMLockTimer = 0;
                    }
                }
            }
            else
            {
                AAMTargetedTime = 0f;
                AAMLockTimer = 0;
                AAMHasTarget = false;
            }
            /*                 Debug.Log(string.Concat("AAMTarget ", AAMTarget));
                            Debug.Log(string.Concat("HasTarget ", AAMHasTarget));
                            Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
                            Debug.Log(string.Concat("LoS ", LineOfSightCur));
                            if (hitcurrent.collider)
                            {
                                Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == OutsideVehicleLayer)));
                                Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
                            }
                            Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
                            Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < AAMLockAngle));
                            Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */
        }
        private void noLock()
        {
            AAMTargetedTime = 0f;//so it plays straight away next time it's targeted
            AAMLockTimer = 0;
            if (AAMHasTarget)
            {
                AAMHasTarget = false;
                RequestSerialization();
            }
        }
        public void Targeted()
        {
            SaccAirVehicle TargetEngine = null;
            if (AAMTargets[AAMTarget] && AAMTargets[AAMTarget].transform.parent)
                TargetEngine = AAMTargets[AAMTarget].transform.parent.GetComponent<SaccAirVehicle>();
            if (TargetEngine)
            {
                if (TargetEngine.Piloting || TargetEngine.Passenger)
                { TargetEngine.VehicleAnimator.SetTrigger("radarlocked"); }
            }
        }
        private void LaunchAAM_Owner()
        {
            if (NumAAM > 0 && AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
            {
                FireNextSerialization = true;
                RequestSerialization();
                LaunchAAM();
                if (LoseLockWhenFired || (NumAAM == 0 && !AllowNoAmmoLock)) { AAMLockTimer = 0; AAMLocked = false; }
                EntityControl.SendEventToExtensions("SFEXT_O_AAMLaunch");
            }
        }
        public void LaunchAAM()
        {
            AAMLastFiredTime = Time.time;
            if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
            AAGunAnimator.SetTrigger("aamlaunched");
            GameObject NewAAM;
            if (transform.childCount - NumChildrenStart > 0)
            { NewAAM = transform.GetChild(NumChildrenStart).gameObject; }
            else
            { NewAAM = InstantiateWeapon(); }
            NewAAM.transform.SetParent(null);
            if (!(NumAAM % 2 == 0))
            {
                //invert local x coordinates of launch point, launch, then revert, for odd numbered shots
                Vector3 temp = AAMLaunchPoint.localPosition;
                temp.x *= -1;
                AAMLaunchPoint.localPosition = temp;
                NewAAM.transform.position = AAMLaunchPoint.transform.position;
                NewAAM.transform.rotation = AAMLaunchPoint.transform.rotation;
                temp.x *= -1;
                AAMLaunchPoint.localPosition = temp;
            }
            else
            {
                NewAAM.transform.position = AAMLaunchPoint.transform.position;
                NewAAM.transform.rotation = AAMLaunchPoint.transform.rotation;
            }
            NewAAM.SetActive(true);
            NewAAM.GetComponent<Rigidbody>().velocity = Vector3.zero;

            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
        }
        public void ReloadAAM()
        {
            AAMReloadTimer = 0;
            NumAAM++;
            if (NumAAM > FullAAMs) { NumAAM = FullAAMs; }
            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
        }
        public void HPRepair()
        {
            Health += HPRepairAmount;
            if (Health == FullHealth)
            { HPRepairTimer = float.MinValue; }
            else { HPRepairTimer = 0; }
            if (Health > FullHealth) { Health = FullHealth; }
            AAGunAnimator.SetFloat("health", Health * FullHealthDivider);
        }
        public void AI_GUN_Enter()
        {
            AI_GUN_RUNNINGLOCAL = true;
            RotationSpeedX = 0;
            RotationSpeedY = 0;
            if (AAGunAnimator) AAGunAnimator.SetBool("inside", true);
            if (HUDControl) { HUDControl.GUN_TargetSpeedLerper = 0; }

            //Make sure AAMCurrentTargetSAVControl is correct
            var Target = AAMTargets[AAMTarget];
            if (Target && Target.transform.parent)
            {
                AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
            }
            RequestSerialization();
        }
        public void AI_GUN_Exit()
        {
            AI_GUN_RUNNINGLOCAL = false;
            AAMHasTarget = false;
            if (AAGunAnimator) AAGunAnimator.SetBool("inside", false);
            RequestSerialization();
        }
        public void SFEXT_O_PilotEnter()
        {
            Manning = true;
            RotationSpeedX = 0;
            RotationSpeedY = 0;
            InVR = EntityControl.InVR;
            if (AAGunAnimator) AAGunAnimator.SetBool("inside", true);
            if (HUDControl) { HUDControl.GUN_TargetSpeedLerper = 0; }

            //Make sure AAMCurrentTargetSAVControl is correct
            var Target = AAMTargets[AAMTarget];
            if (Target && Target.transform.parent)
            {
                AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
            }
            if (localPlayer != null)
            {
                InVR = localPlayer.IsUserInVR();//has to be set on enter otherwise Built And Test thinks you're in desktop
            }
            RequestSerialization();
            if (RotatingSound) { RotatingSound.Play(); }
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            //Reload based on the amount of time passed while no one was inside
            float TimeSinceLast = (Time.time - LastHealthUpdate);
            LastHealthUpdate = Time.time;

            NumAAM = Mathf.Min((NumAAM + ((int)(TimeSinceLast / MissileReloadTime))), FullAAMs);
            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
            Health = Mathf.Min((Health + (((int)(TimeSinceLast / HPRepairDelay))) * HPRepairAmount), FullHealth);
            AAGunAnimator.SetFloat("health", Health * FullHealthDivider);
            MGAmmoRecharge += TimeSinceLast * MGReloadSpeed;
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            LastHealthUpdate = Time.time;
            if (Firing)
            { Firing = false; }
        }
        public void SFEXT_O_PilotExit()
        {
            Manning = false;
            AAMLockTimer = 0;
            AAMHasTarget = false;
            AAMTargeting.gameObject.SetActive(false);
            AAMTargetLock.gameObject.SetActive(false);
            AAGunAnimator.SetBool("inside", false);
            if (RotatingSound) { RotatingSound.Stop(); }
            if (AI_GUN) { AI_GUN_Enter(); }
            RequestSerialization();
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (!Occupied)//if we took ownership by getting in, don't do this
            {
                if (Firing)
                {
                    Firing = false;
                    RequestSerialization();
                }
            }
            if (AI_GUN) { AI_GUN_Enter(); }
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            AI_GUN_Exit();
        }
        public void SFEXT_L_DamageFeedback()
        {
            if (DamageFeedBack && !AI_GUN_RUNNINGLOCAL) { DamageFeedBack.PlayOneShot(DamageFeedBack.clip); }
        }
        private bool FireNextSerialization = false;
        public override void OnPreSerialization()
        {
            if (FireNextSerialization)
            {
                FireNextSerialization = false;
                AAMFireNow = true;
            }
            else { AAMFireNow = false; }
            if (Time.time - SendTargeted_Time < SENDTARGETED_INTERVAL)
            { SendTargeted = true; }
            else { SendTargeted = false; }
        }
        public override void OnDeserialization()
        {
            if (AAMFireNow) { LaunchAAM(); }
            if (SendTargeted)
            {
                if (!Manning)
                {
                    var Target = AAMTargets[AAMTarget];
                    if (Target && Target.transform.parent)
                    {
                        AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
                    }
                    if (AAMCurrentTargetSAVControl != null && AAMCurrentTargetSAVControl.EntityControl.InVehicle)
                    { AAMCurrentTargetSAVControl.EntityControl.SendEventToExtensions("SFEXT_L_AAMTargeted"); }
                }
            }
        }
    }
}