
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
        public VRCStation AAGunSeat;
        [Tooltip("Missile object to be duplicated and enabled when a missile is fired")]
        public GameObject AAM;
        [Range(0, 2)]
        [Tooltip("0 = Radar, 1 = Heat, 2 = Other. Controls what variable is added to in SaccAirVehicle to count incoming missiles, AND which variable to check for reduced tracking, (MissilesIncomingHeat NumActiveFlares, MissilesIncomingRadar NumActiveChaff, MissilesIncomingOther NumActiveOtherCM)")]
        public int MissileType = 1;
        [Tooltip("Audio source that plays when rotating")]
        public AudioSource RotatingSound;
        public float RotatingSoundMulti = .02f;
        [Tooltip("Sound that plays when targeting an enemy")]
        public AudioSource AAMLocking;
        [Tooltip("Sound that plays when locked onto a target")]
        public AudioSource AAMLockedOn;
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
        [Range(0, 1)]
        public float TurnFriction = .04f;
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
        [Tooltip("Minimum time between missile launches")]
        public float AAMLaunchDelay = 0f;
        [Tooltip("Point missile is launched from, flips on local X each time fired")]
        public Transform AAMLaunchPoint;
        [Tooltip("Layer to spherecast to find all triggers on to use as AAM targets")]
        public LayerMask AAMTargetsLayer;
        [Tooltip("Tick this to disable target tracking, prediction, and missiles (WW2 flak?)")]
        public bool DisableAAMTargeting;
        [Tooltip("Layer vehicles to shoot at are on (for raycast to check for line of sight)")]
        public float PlaneHitBoxLayer = 17;//walkthrough
        [Tooltip("Multiplies how much damage is taken from bullets")]
        public float BulletDamageTaken = 10f;
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
        private Vector3 StartRot;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool IsOwner = false;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
        [HideInInspector] public GameObject[] AAMTargets;
        [HideInInspector] public int NumAAMTargets = 0;
        private int AAMTargetChecker = 0;
        [System.NonSerializedAttribute] public bool AAMHasTarget = false;
        private float AAMTargetedTimer = 2f;
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
                if (!EntityControl._dead && Occupied)
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
        [UdonSynced, FieldChangeCallback(nameof(AAMFire))] private short _AAMFire;
        public short AAMFire
        {
            set
            {
                _AAMFire = value;
                if (!EntityControl._dead && Occupied)
                { LaunchAAM(); }
            }
            get => _AAMFire;
        }
        [UdonSynced, FieldChangeCallback(nameof(sendtargeted))] private bool _SendTargeted;
        public bool sendtargeted
        {
            set
            {
                if (!Manning)
                {
                    var Target = AAMTargets[AAMTarget];
                    if (Target && Target.transform.parent)
                    {
                        AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
                    }
                    if (AAMCurrentTargetSAVControl != null)
                    { AAMCurrentTargetSAVControl.EntityControl.SendEventToExtensions("SFEXT_L_AAMTargeted"); }
                }
                _SendTargeted = value;
            }
            get => _SendTargeted;
        }
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) { InEditor = true; Manning = true; IsOwner = true; }
            else
            {
                InEditor = false;
                InVR = localPlayer.IsUserInVR();
                IsOwner = localPlayer.isMaster;
            }
            CenterOfMass = EntityControl.CenterOfMass;

            AAGunAnimator = EntityControl.GetComponent<Animator>();
            FullHealth = Health;
            FullHealthDivider = 1f / (Health > 0 ? Health : 10000000);
            StartRot = Rotator.localRotation.eulerAngles;
            if (RotatingSound) { RotateSoundVol = RotatingSound.volume; }

            FullAAMs = NumAAM;
            FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
            MGAmmoFull = MGAmmoSeconds;
            FullMGDivider = 1f / (MGAmmoFull > 0 ? MGAmmoFull : 10000000);

            AAMTargets = EntityControl.AAMTargets;
            NumAAMTargets = AAMTargets.Length;
            if (NumAAMTargets != 0 && !DisableAAMTargeting) { DoAAMTargeting = true; }
            gameObject.SetActive(true);


            NumChildrenStart = transform.childCount;
            int NumToInstantiate = Mathf.Min(FullAAMs, 10);
            for (int i = 0; i < NumToInstantiate; i++)
            {
                InstantiateWeapon();
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(AAM);
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

                    RotationSpeedX += -(RotationSpeedX * TurnFriction) + (InputX);
                    RotationSpeedY += -(RotationSpeedY * TurnFriction) + (InputY);

                    //rotate turret
                    Vector3 rot = Rotator.localRotation.eulerAngles;
                    float NewX = rot.x;
                    NewX += RotationSpeedX * DeltaTime;
                    if (NewX > 180) { NewX -= 360; }
                    if (NewX > DownAngleMax || NewX < -UpAngleMax) RotationSpeedX = 0;
                    NewX = Mathf.Clamp(NewX, -UpAngleMax, DownAngleMax);//limit angles
                    float NewY = rot.y + (RotationSpeedY * DeltaTime);
                    Rotator.localRotation = Quaternion.Euler(new Vector3(NewX, NewY, 0));
                    //Firing the gun
                    if ((RTrigger >= 0.75 || Input.GetKey(KeyCode.Space)) && MGAmmoSeconds > 0)
                    {
                        if (!_firing)
                        {
                            Firing = true;
                            RequestSerialization();
                            if (IsOwner)
                            { EntityControl.SendEventToExtensions("SFEXT_O_GunStartFiring"); }
                        }
                        MGAmmoSeconds -= DeltaTime;
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
                        MGAmmoRecharge = Mathf.Min(MGAmmoRecharge + (DeltaTime * MGReloadSpeed), MGAmmoFull);
                        MGAmmoSeconds = Mathf.Max(MGAmmoRecharge, MGAmmoSeconds);
                    }

                    if (DoAAMTargeting)
                    {
                        if (AAMLockTimer > AAMLockTime && AAMHasTarget) AAMLocked = true;
                        else { AAMLocked = false; }
                        //firing AAM
                        if (LTrigger > 0.75 || (Input.GetKey(KeyCode.C)))
                        {
                            if (!LTriggerLastFrame)
                            {
                                if (AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                                {
                                    AAMLastFiredTime = Time.time;
                                    AAMFire++;//launch AAM using set
                                    RequestSerialization();
                                    if (NumAAM == 0) { AAMLockTimer = 0; AAMLocked = false; }
                                }
                            }
                            LTriggerLastFrame = true;
                        }
                        else LTriggerLastFrame = false;
                    }

                    //reloading AAMs
                    if (NumAAM == FullAAMs)
                    { AAMReloadTimer = 0; }
                    else
                    { AAMReloadTimer += DeltaTime; }
                    if (AAMReloadTimer > MissileReloadTime)
                    {
                        if (InEditor)
                        { ReloadAAM(); }
                        else
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReloadAAM)); }
                    }
                    //HP Repair
                    if (Health == FullHealth)
                    { HPRepairTimer = 0; }
                    else
                    { HPRepairTimer += DeltaTime; }
                    if (HPRepairTimer > HPRepairDelay)
                    {
                        if (InEditor)
                        { HPRepair(); }
                        else
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(HPRepair)); }
                    }
                    //Sounds
                    if (AAMLockTimer > 0 && !AAMLocked && NumAAM > 0)
                    {
                        AAMLocking.gameObject.SetActive(true);
                        AAMLockedOn.gameObject.SetActive(false);
                    }
                    else if (AAMLocked)
                    {
                        AAMLocking.gameObject.SetActive(false);
                        AAMLockedOn.gameObject.SetActive(true);
                    }
                    else
                    {
                        AAMLocking.gameObject.SetActive(false);
                        AAMLockedOn.gameObject.SetActive(false);
                    }
                    if (RotatingSound)
                    {
                        float turnvol = new Vector2(RotationSpeedX, RotationSpeedY).magnitude * RotatingSoundMulti;
                        RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                        RotatingSound.pitch = turnvol;
                    }
                }
            }
        }
        private void FixedUpdate()
        {
            if (Manning && DoAAMTargeting)
            {
                AAMTargeting(AAMLockAngle);
            }
        }
        public void Explode()//all the things players see happen when the vehicle explodes
        {
            if (EntityControl._dead) { return; }
            if (Manning && !InEditor)
            {
                if (AAGunSeat) { AAGunSeat.ExitStation(localPlayer); }
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
                Rotator.localRotation = Quaternion.Euler(StartRot);
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
                    PredictedHealth = Health - (BulletDamageTaken * EntityControl.LastHitBulletDamageMulti);
                    LastHitTime = Time.time;//must be updated before sending explode() for checks in explode event to work
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                    }
                }
                else
                {
                    PredictedHealth -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    LastHitTime = Time.time;
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
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
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                }
            }
        }
        private void AAMTargeting(float Lock_Angle)
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
                    bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);

                    /* Debug.Log(string.Concat("LoS ", LineOfSightNext));
                    Debug.Log(string.Concat("RayCastLayer ", hitnext.collider.gameObject.layer == PlaneHitBoxLayer));
                    Debug.Log(string.Concat("InAngle ", NextTargetAngle < Lock_Angle));
                    Debug.Log(string.Concat("BelowMaxDist ", NextTargetDistance < AAMMaxTargetDistance));
                    Debug.Log(string.Concat("LowerAngle ", NextTargetAngle < AAMCurrentTargetAngle));
                    Debug.Log(string.Concat("CurrentTargTaxiing ", !AAMCurrentTargetSAVControlNull && AAMCurrentTargetSAVControl.Taxiing)); */
                    if ((LineOfSightNext
                        && hitnext.collider && hitnext.collider.gameObject.layer == PlaneHitBoxLayer
                            && NextTargetAngle < Lock_Angle
                                && NextTargetDistance < AAMMaxTargetDistance
                                    && NextTargetAngle < AAMCurrentTargetAngle)
                                        || ((AAMCurrentTargetSAVControl && AAMCurrentTargetSAVControl.Taxiing) || !AAMTargets[AAMTarget].activeInHierarchy)) //prevent being unable to target next target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                    {
                        //found new target
                        AAMCurrentTargetAngle = NextTargetAngle;
                        AAMTarget = AAMTargetChecker;
                        AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                        AAMCurrentTargetSAVControl = NextTargetSAVControl;
                        AAMLockTimer = 0;
                        AAMTargetedTimer = 99f;//send targeted straight away
                        if (HUDControl)
                        {
                            HUDControl.RelativeTargetVelLastFrame = Vector3.zero;
                            HUDControl.GUN_TargetSpeedLerper = 0f;
                            HUDControl.GUN_TargetDirOld = AAMNextTargetDirection * 1.00001f; //so the difference isn't 0
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
            bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
            //used to make lock remain for .25 seconds after target is obscured
            if (!LineOfSightCur || (hitcurrent.collider && hitcurrent.collider.gameObject.layer != PlaneHitBoxLayer))
            { AAMTargetObscuredDelay += DeltaTime; }
            else
            { AAMTargetObscuredDelay = 0; }
            if (AAMTargets[AAMTarget].activeInHierarchy
                    && (!AAMCurrentTargetSAVControl || (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl._dead)))
            {
                if ((AAMTargetObscuredDelay < .25f)
                            && AAMCurrentTargetDistance < AAMMaxTargetDistance)
                {
                    AAMHasTarget = true;
                    if (AAMCurrentTargetAngle < Lock_Angle && NumAAM > 0)
                    {
                        AAMLockTimer += DeltaTime;
                        //dont give enemy radar lock if you're out of missiles (planes do do this though)
                        if (AAMCurrentTargetSAVControl)
                        {
                            //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                            AAMTargetedTimer += DeltaTime;
                            if (AAMTargetedTimer > 1)
                            {
                                sendtargeted = !sendtargeted;
                                RequestSerialization();
                                AAMTargetedTimer = 0;
                            }
                        }
                    }
                    else
                    {
                        AAMTargetedTimer = 2f;
                        AAMLockTimer = 0;
                    }
                }
                else
                {
                    AAMTargetedTimer = 2f;//so it plays straight away next time it's targeted
                    AAMLockTimer = 0;
                    AAMHasTarget = false;
                }
            }
            else
            {
                AAMTargetedTimer = 2f;
                AAMLockTimer = 0;
                AAMHasTarget = false;
            }
            /*Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
            Debug.Log(string.Concat("LoS ", LineOfSightCur));
            Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == PlaneHitBoxLayer)));
            Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
            Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
            Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < Lock_Angle));
            Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */
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
        public void LaunchAAM()
        {
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
        public void SFEXT_O_PilotEnter()
        {
            Manning = true;
            RotationSpeedX = 0;
            RotationSpeedY = 0;
            if (AAGunAnimator) AAGunAnimator.SetBool("inside", true);
            if (HUDControl) { HUDControl.GUN_TargetSpeedLerper = 0; }

            //Make sure SAVControl.AAMCurrentTargetSAVControl is correct
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
            //This function is called by OnStationEntered(), currently there's a bug where OnStationEntered() is called multiple times, when entering a seat
            //this check stops it from doing anything more than once
            if (TimeSinceLast > 1)
            {
                NumAAM = Mathf.Min((NumAAM + ((int)(TimeSinceLast / MissileReloadTime))), FullAAMs);
                AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
                Health = Mathf.Min((Health + (((int)(TimeSinceLast / HPRepairDelay))) * HPRepairAmount), FullHealth);
                AAGunAnimator.SetFloat("health", Health * FullHealthDivider);
                MGAmmoRecharge += TimeSinceLast * MGReloadSpeed;
            }
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
            AAMLocking.gameObject.SetActive(false);
            AAMLockedOn.gameObject.SetActive(false);
            AAGunAnimator.SetBool("inside", false);
            if (RotatingSound) { RotatingSound.Stop(); }
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_L_OwnershipTransfer()
        {
            SendCustomEventDelayedSeconds(nameof(CheckOwnership), .2f);
        }
        //if took ownership after someone timed out while in vehicle, turn off this stuff
        public void CheckOwnership()
        {
            if (!Occupied)//if we took ownership by getting in, don't do this
            {
                if (Firing)
                {
                    Firing = false;
                }
            }
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_L_DamageFeedback()
        {
            if (DamageFeedBack) { DamageFeedBack.PlayOneShot(DamageFeedBack.clip); }
        }
    }
}