
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_AAMController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour AAMLauncherControl;
        UdonSharpBehaviour SAVControl;
        [Tooltip("1 = direct hit takes 100% of target health.\n If AAMDamage_AbsoluteMode is true, 1 = 1 damage with direct hit.")]
        public float AAMDamage = 1;
        [Tooltip("Enable this to stop AAMDamage being multiplied by the vehicle's full health. See AAMDamge tooltip.")]
        public bool AAMDamage_AbsoluteMode = false;
        [Tooltip("Missile will explode after this time")]
        public float MaxLifetime = 12;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        public float ExplosionLifeTime = 10;
        [Tooltip("Strength of the effect of countermeasures on the missile (ignore 'Flare', it's the effect of whatever countermeasure type this missile is effected by")]
        public float FlareEffect = 10;
        [Tooltip("For simulating FOX-1/3 missiles, (Requires rigidbody on vehicle, may not work for AAGun)")]
        public bool RequireParentLock = false;
        [Tooltip("For simulating FOX-3. Missile will not require parent vehicle lock after it is closer to target than this distance. Unlike a real FOX-3, it will only chase it's original target in pitbull mode. Meters. Set to 0 for FOX-1")]
        public float PitBullDistance = 0;
        [Range(0, 180f)]
        [Tooltip("If angle of missile velocity to target vector is greater than this, go dumb")]
        public float MaxTrackingAngle = 90;
        [Range(0, 180f)]
        [Tooltip("If the missile and target vehicle are facing towards each other, multiply rotation speed by HighAspectRotSpeedMulti with this nose angle (facing perfectly towards each other = 0 degrees, which is the same as disabled) Set 0 for any non-heatseeker missiles")]
        public float HighAspectTrackAngle = 60;
        [Tooltip("See above")]
        public float HighAspectRotSpeedMulti = .5f;
        [Tooltip("Send the target plane's animator an integer +1 whilst this missile is flying towards it")]
        public bool SendMissileIncoming = true;
        [Tooltip("Name of animator integer to +1 on the target plane while chasing it")]
        public string MissileIncomingName = "missilesincoming";
        [Tooltip("Play a random one of these explosion sounds")]
        public AudioSource[] ExplosionSounds;
        [Tooltip("Play a random one of these explosion sounds when hitting water")]
        public AudioSource[] WaterExplosionSounds;
        [Tooltip("Distance from own plane to enable the missile's collider, to prevent missile from colliding with own plane")]
        public float ColliderActiveDistance = 45;
        [Tooltip("Speed missile can rotate in degrees per second")]
        public float RotSpeed = 180;
        [Range(1.01f, 2f)]
        [Tooltip("Amount the target direction vector is extended when calculating missile rotation. Lower number = more aggressive drifting missile, but more likely to oscilate")]
        public float TargetVectorExtension = 1.2f;
        [Tooltip("Maxmimum extrapolation distance in seconds for target interception, to prevent distant missiles getting confused too easily")]
        public float MaximumExtrapTime = 3f;
        [Tooltip("If target vehicle has afterburner on, multiply rotation speed by this value")]
        public float AfterBurnerTrackMulti = 2f;
        [Tooltip("Missile rotates weaker if target's throttle is low, this value is the throttle at which lowering throttle more doesn't do anything")]
        public float TargetMinThrottleTrack = .3f;
        [Tooltip("When passing target, if within this range, explode")]
        public float ProximityExplodeDistance = 20;
        [Tooltip("How long after being unable to track the target this missile gives up completely and will never track again")]
        public float UnlockTime = .1f;
        [Tooltip("Lockhack stops the missile from being able to lose lock before a certain amount of time has passed after it starts tracking for people who didn't fire it. It ensures the missile tracks its target in cases where the firer's position is desynced badly. Very necessary when using VRC_ObjectSync.")]
        public float LockHackTime = .1f;
        [Tooltip("How long after launch the missile should start tracking")]
        public float FlyStraightTime = .3f;
        [Tooltip("Strength of the forces applied to the sides of the missiles as it drifts through the air when it turns")]
        public float AirPhysicsStrength = .8f;
        [Tooltip("Missile predicts movement of target and tries to intercept rather than flying towards targets current position")]
        public bool PredictiveChase = true;
        [Range(0, 90f)]
        [Tooltip("Closeness in degrees to perpendicular the missile must be to be notched 0 = no notching")]
        public float NotchAngle = 0;

        [Range(-90f, 90f)]
        [Tooltip("Degrees above the missile's horizon at which notching the missile becomes impossible")]
        public float NotchHorizon = 5;
        [Tooltip("This velocity is added to the missile when it spawns.")]
        public Vector3 ThrowVelocity = new Vector3(0, 0, 0);
        [Tooltip("Enable this tickbox to make the ThrowVelocity vector local to the vehicle instead of the missile")]
        public bool ThrowSpaceVehicle = true;
        private string[] MissileTypes = { "MissilesIncomingRadar", "MissilesIncomingHeat", "MissilesIncomingOther" };//names of variables in SaccAirVehicle
        private string[] CMTypes = { "NumActiveChaff", "NumActiveFlares", "NumActiveOtherCM" };//names of variables in SaccAirVehicle

        [Tooltip("event_WeaponType is sent with damage and kill events, but not used for anything in the base prefab.\n0=None/Suicide,1=Gun,2=AAM,3=AGM,4=Bomb,5=Rocket,6=Cannon,7=Laser,8=Beam,9=Torpedo,10=VLS,11=Javelin,12=Railgun, anything else is undefined (custom) 0-255")]
        [SerializeField] private byte event_WeaponType = 2;
        [System.NonSerialized] public SaccEntity EntityControl;
        Transform VehicleTransform;
        private int MissileType = 1;
        private SaccAirVehicle TargetSAVControl;
        private Animator TargetAnimator;
        SaccEntity TargetEntityControl;
        private bool LockHack = true;
        private bool PitBull = false;
        private bool DoPitBull = false;
        [System.NonSerializedAttribute] public Transform Target;
        private bool ColliderActive = false;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private Collider AAMCollider;
        private bool MissileIncoming = false;
        private Rigidbody AAMRigid;
        private Rigidbody VehicleRigid;
        private float TargDistlastframe = 999999999;
        private bool TargetLost = false;
        private float UnlockTimer;
        private float TargetABPoint;
        private float TargetThrottleNormalizer;
        Vector3 TargetPosLastFrame;

        private bool IsOwner;
        private bool InEditor;
        private bool Initialized = false;
        private bool DirectHit = false;
        private bool SplashHit = false;
        private bool StartTrack = false;
        private float HighAspectTrack;
        private float NotchHorizonDot;
        private float NotchLimitDot;
        private bool hitwater;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private GameObject PitBullIndicator;
        private Animator MissileAnimator;
        GameObject[] AAMTargets;
        private Vector3 LastRealPos;
        private Vector3 PredictedPos;
        private float AAMMaxTargetDistance;
        private int OutsideVehicleLayer;
        Vector3 LocalLaunchPoint;
        private bool ColliderAlwaysActive;
        bool isNotHeat;
        void Initialize()
        {
            EntityControl = (SaccEntity)AAMLauncherControl.GetProgramVariable("EntityControl");
            SAVControl = (UdonSharpBehaviour)AAMLauncherControl.GetProgramVariable("SAVControl");
            VehicleTransform = EntityControl.transform;
            //whatever script is launching the missiles must contain all of these variables
            InEditor = (bool)AAMLauncherControl.GetProgramVariable("InEditor");
            MissileAnimator = GetComponent<Animator>();
            AAMRigid = GetComponent<Rigidbody>();
            VehicleRigid = EntityControl.VehicleRigidbody;
            AAMCollider = GetComponent<Collider>();
            MissileType = (int)AAMLauncherControl.GetProgramVariable("MissileType");
            PitBullIndicator = (GameObject)AAMLauncherControl.GetProgramVariable("PitBullIndicator");
            AAMMaxTargetDistance = (float)AAMLauncherControl.GetProgramVariable("AAMMaxTargetDistance");

            DoPitBull = PitBullDistance > 0f;
            NotchHorizonDot = 1 - Mathf.Cos(NotchHorizon * Mathf.Deg2Rad);//angle as dot product
            NotchLimitDot = 1 - Mathf.Cos(NotchAngle * Mathf.Deg2Rad);
            HighAspectTrack = Mathf.Cos(HighAspectTrackAngle * Mathf.Deg2Rad);
            ColliderAlwaysActive = ColliderActiveDistance == 0;

            isNotHeat = MissileType != 1;
        }
        public void StartTracking()
        {
            StartTrack = true;
        }
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
            if (EntityControl.InEditor) { IsOwner = true; }
            else { IsOwner = (bool)AAMLauncherControl.GetProgramVariable("IsOwner"); }
            if (ColliderAlwaysActive) { AAMCollider.enabled = true; ColliderActive = true; }
            else { AAMCollider.enabled = false; ColliderActive = false; }
            LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(transform.position - EntityControl.transform.position);
            AAMRigid.velocity = AAMRigid.velocity + (ThrowSpaceVehicle ? EntityControl.transform.TransformDirection(ThrowVelocity) : transform.TransformDirection(ThrowVelocity));
            int aamtarg = (int)AAMLauncherControl.GetProgramVariable("AAMTarget");
            AAMTargets = (GameObject[])AAMLauncherControl.GetProgramVariable("AAMTargets");
            Target = AAMTargets[aamtarg].transform;

            if (ColliderAlwaysActive && !EntityControl.IsOwner && AAMRigid && !AAMRigid.isKinematic && SAVControl)
            {
                // because non-owners update position of vehicle in Update() via SyncScript, it can clip into the projectile before next physics update
                // So in the updates until then move projectile by vehiclespeed
                ensureNoSelfCollision_time = Time.fixedTime;
                ensureNoSelfCollision();
            }

            //FixedUpdate runs one time after MoveBackToPool so these must be here
            ColliderActive = false;
            DirectHit = false;
            SplashHit = false;
            LockHack = true;
            TargetLost = false;
            MissileIncoming = false;
            PitBull = false;
            UnlockTimer = 0;
            if (!Target)
            {
                TargetLost = true;
                Debug.LogWarning("AAM spawned without target");
            }
            else
            {
                TargDistlastframe = Vector3.Distance(transform.position, Target.position) + 1;//1 meter further so the number is different and missile knows we're already moving toward target
                TargetPosLastFrame = Target.position - Target.forward;//assume enemy plane was 1 meter behind where it is now last frame because we don't know the truth
                if (Target.parent)
                {
                    TargetSAVControl = Target.parent.GetComponent<SaccAirVehicle>();
                    if (TargetSAVControl)
                    {
                        TargetEntityControl = (SaccEntity)TargetSAVControl.GetProgramVariable("EntityControl");
                        if (TargetEntityControl)
                            OutsideVehicleLayer = TargetEntityControl.OutsideVehicleLayer;
                        else
                            OutsideVehicleLayer = 17; //walkthrough    
                        if (SendMissileIncoming && ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger")))
                        {
                            if (!MissileIncoming)
                            {
                                TargetSAVControl.SetProgramVariable(MissileTypes[MissileType], (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]) + 1);
                                MissileIncoming = true;
                            }
                        }
                        TargetAnimator = (Animator)TargetSAVControl.GetProgramVariable("VehicleAnimator");
                        TargetAnimator.SetInteger(MissileIncomingName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                        TargetABPoint = (float)TargetSAVControl.GetProgramVariable("ThrottleAfterburnerPoint");
                        TargetThrottleNormalizer = 1 / TargetABPoint;
                    }
                    else
                        OutsideVehicleLayer = 17; //walkthrough    
                }

                if (InEditor || IsOwner || LockHackTime == 0)
                { LockHack = false; }
                else
                { SendCustomEventDelayedSeconds(nameof(DisableLockHack), FlyStraightTime + LockHackTime); }
            }
            Initialized = true;
            SendCustomEventDelayedSeconds(nameof(StartTracking), FlyStraightTime);
            SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
            LifeTimeExplodesSent++;
        }
        float ensureNoSelfCollision_time;
        public void ensureNoSelfCollision()
        {
            if (ensureNoSelfCollision_time != Time.fixedTime) return;

            transform.position += (Vector3)SAVControl.GetProgramVariable("CurrentVel") * Time.deltaTime;
            AAMRigid.position = transform.position;
            SendCustomEventDelayedFrames(nameof(ensureNoSelfCollision), 1);
        }
        void FixedUpdate()
        {
            if (Exploding) return;
            float sidespeed = Vector3.Dot(AAMRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(AAMRigid.velocity, transform.up);
            AAMRigid.AddRelativeForce(new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, 0), ForceMode.Acceleration);
            float DeltaTime = Time.fixedDeltaTime;
            if (!ColliderActive && Initialized)
            {
                Vector3 LaunchPoint = VehicleRigid ?
                    (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position :
                    (VehicleTransform.rotation * LocalLaunchPoint) + VehicleTransform.position
                ;
                if (Vector3.Distance(AAMRigid.position, LaunchPoint) > ColliderActiveDistance)
                {
                    AAMCollider.enabled = true;
                    ColliderActive = true;
                }
            }
            if (!TargetLost && StartTrack)
            {
                Vector3 Position = transform.position;
                Vector3 TargetPos = Target.position;
                float TargetDistance = Vector3.Distance(Position, TargetPos);
                if (DoPitBull)
                {
                    if (!PitBull)
                    {
                        if (TargetDistance < PitBullDistance)
                        {
                            PitBull = true;
                            if (PitBullIndicator)
                            {
                                PitBullIndicator.SetActive(true);
                                pitbullsSent++;
                                SendCustomEventDelayedSeconds(nameof(DisablePitBullIndicator), 1);
                            }
                        }
                    }
                    /*else
                    {
                    //TODO
                    //Search through targets 1 per frame or slower and chase closest/lowest angle target instead
                    //don't forget to send lock alarm to new target and cancel old one
                     } */
                }

                float EngineTrack;
                float AspectTrack;
                bool Dumb;
                Vector3 Targetmovedir;
                TargetPosLastFrame = TargetPos;
                Vector3 MissileToTargetVector;
                if (TargetSAVControl)
                {
                    Targetmovedir = (Vector3)TargetSAVControl.GetProgramVariable("CurrentVel");
                    //other player's vehicles only move on Update() (low framerate fix)
                    if (TargetEntityControl.CenterOfMass.position != LastRealPos)
                    {
                        LastRealPos = TargetEntityControl.CenterOfMass.position;
                        PredictedPos = LastRealPos;
                    }
                    else
                    {
                        PredictedPos += Targetmovedir * Time.fixedDeltaTime;
                    }
                    MissileToTargetVector = (PredictedPos - Position).normalized;
                    bool TargetLineOfSight = CheckTargetLOS();
                    bool MotherLoS = true;
                    if (RequireParentLock && !PitBull) { MotherLoS = CheckMotherLOS(); }
                    Dumb = //Missile just flies straight if it's confused by flares or notched
                           //flare effect
                        Random.Range(0, 100) < (int)TargetSAVControl.GetProgramVariable(CMTypes[MissileType]) * FlareEffect//if there are flares active, there's a chance it will not track per frame.
                        ||
                        //notching
                        //if the target is traveling perpendicular to the direction the missile is looking at it from, it is 'notching' the missile
                        (Vector3.Dot(Vector3.up, MissileToTargetVector) < NotchHorizonDot
                        && Mathf.Abs(Vector3.Dot(Targetmovedir.normalized, MissileToTargetVector)) < NotchLimitDot)
                        ||
                        //FOX-1
                        (RequireParentLock && !PitBull &&
                            (!MotherLoS || Target.gameObject != AAMTargets[(int)AAMLauncherControl.GetProgramVariable("AAMTarget")] || !(bool)AAMLauncherControl.GetProgramVariable("_AAMLocked"))
                        )
                        ||
                        (!TargetLineOfSight && (!RequireParentLock || PitBull))
                        ||
                        Vector3.Angle(MissileToTargetVector, AAMRigid.velocity) > MaxTrackingAngle
                        ;

                    //Heat missiles have a harder time tracking from the front
                    AspectTrack = isNotHeat ? 1 :
                        (Vector3.Dot(MissileToTargetVector, -TargetEntityControl.transform.forward) > HighAspectTrack ? HighAspectRotSpeedMulti : 1);
                    //Heat missiles track more weakly if engine is low (unless wrecked (on fire))
                    EngineTrack = (TargetSAVControl.EntityControl.wrecked || isNotHeat) ? 1 :
                        (Mathf.Max((float)TargetSAVControl.GetProgramVariable("EngineOutput") * TargetThrottleNormalizer, TargetMinThrottleTrack));
                }
                else
                {
                    bool MotherLoS = true;
                    if (RequireParentLock && !PitBull) { MotherLoS = CheckMotherLOS(); }
                    MissileToTargetVector = (TargetPos - Position).normalized;
                    CheckTargetLOS();
                    Targetmovedir = (TargetPos - TargetPosLastFrame) / DeltaTime;
                    EngineTrack = 1;
                    AspectTrack = 1;
                    Dumb =
                        (RequireParentLock && !PitBull &&
                            (!MotherLoS || Target.gameObject != AAMTargets[(int)AAMLauncherControl.GetProgramVariable("AAMTarget")] || !(bool)AAMLauncherControl.GetProgramVariable("_AAMLocked"))
                        )
                        ||
                        Vector3.Angle(MissileToTargetVector, AAMRigid.velocity) > MaxTrackingAngle
                        ;
                }
                if (EngineTrack > 1) { EngineTrack = AfterBurnerTrackMulti; }//if AB on, faster rotation
                if (Target.gameObject.activeInHierarchy && UnlockTimer < UnlockTime)
                {
                    if (!Dumb || LockHack)
                    {
                        if (PredictiveChase)
                        {
                            float timetotarget = Mathf.Min(TargetDistance / Mathf.Max(((TargDistlastframe - TargetDistance) / DeltaTime), 0.001f), MaximumExtrapTime);//ensure no division by 0
                            Vector3 TargetPredictedPos = TargetPos + ((Targetmovedir * timetotarget));
                            MissileToTargetVector = TargetPredictedPos - Position;
                        }
                        //else using the already set targdirection
                        UnlockTimer = 0;
                        //turn towards the target
                        Vector3 TargetDirNormalized = MissileToTargetVector.normalized * TargetVectorExtension;
                        Vector3 MissileVelNormalized = AAMRigid.velocity.normalized;
                        Vector3 MissileForward = transform.forward;
                        Vector3 targetDirection = TargetDirNormalized - MissileVelNormalized;
                        Vector3 RotationAxis = Vector3.Cross(MissileForward, targetDirection);
                        float deltaAngle = Vector3.Angle(MissileForward, targetDirection);
                        transform.Rotate(RotationAxis, Mathf.Min(RotSpeed * EngineTrack * AspectTrack * DeltaTime, deltaAngle), Space.World);
                        AAMRigid.rotation = transform.rotation;
                    }
                    else
                    {
                        if (TargetDistance < ProximityExplodeDistance && Target && Target.gameObject.activeInHierarchy)//missile flew past the target, but is within proximity explode range?
                        {
                            SplashHit = true;
                            hitwater = false;
                            Explode();
                        }
                        UnlockTimer += DeltaTime;
                    }
                }
                else
                {
                    TargetLost = true;
                    if (MissileIncoming)
                    {
                        //just flew past the target, stop missile warning sound
                        if ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger"))
                        {
                            TargetSAVControl.SetProgramVariable(MissileTypes[MissileType], (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]) - 1);
                            TargetAnimator.SetInteger(MissileIncomingName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                        }
                        MissileIncoming = false;
                    }
                }
                TargDistlastframe = TargetDistance;
            }
        }
        bool CheckMotherLOS()
        {
            //This function requires the mothership to have a rigidbody. (AAGuns will not work)
            RaycastHit rayHit;
            if (MissileType == 0 && !PitBull) // Fox-1 requires LoS to mother vehicle instead of target to recieve target data
            {
                Vector3 dir = EntityControl.CenterOfMass.position - transform.position;
                if (Physics.Raycast(transform.position, dir, out rayHit, dir.magnitude, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide))
                {
                    if (rayHit.collider.attachedRigidbody && VehicleRigid)
                    {
                        if (rayHit.collider.attachedRigidbody == VehicleRigid) return true;
                        else return false;
                    }
                }
                else return true; // the ray terminated at our center of mass so it reached us, it's just not checking the onboardvehiclelayer
            }
            return false;
        }
        bool CheckTargetLOS()
        {
            RaycastHit rayHit;
            Vector3 targdir;
            if (TargetSAVControl)
                targdir = TargetEntityControl.CenterOfMass.position - transform.position;
            else
                targdir = Target.position - transform.position;
            if (Physics.Raycast(transform.position, targdir, out rayHit, targdir.magnitude, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide))
            {
                if (rayHit.collider && (rayHit.collider.gameObject.layer == OutsideVehicleLayer || rayHit.collider.gameObject.layer == EntityControl.OnboardVehicleLayer))
                { return true; }
                else
                { return false; }
            }
            else return true; // the ray terminated at our center of mass so it reached us, it's just not checking the onboardvehiclelayer
        }
        public void DisableLockHack()
        { LockHack = false; }
        uint pitbullsSent;
        public void DisablePitBullIndicator()
        {
            pitbullsSent--;
            if (pitbullsSent != 0) return;
            if (PitBullIndicator) { PitBullIndicator.SetActive(false); }
        }
        public void LifeTimeExplode()
        {
            //prevent the delayed event from a previous life causing explosion
            if (LifeTimeExplodesSent == 1)
            {
                if (!Exploding && gameObject.activeSelf)//active = not in pool
                { hitwater = false; Explode(); }
            }
            LifeTimeExplodesSent--;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other && other.gameObject.layer == 4 /* water */)
            {
                if (!Exploding)
                {
                    hitwater = true;
                    Explode();
                }
            }
        }
        public void MoveBackToPool()
        {
            MissileAnimator.WriteDefaultValues();
            gameObject.SetActive(false);
            transform.SetParent(AAMLauncherControl.transform);
            AAMCollider.enabled = false;
            AAMRigid.constraints = RigidbodyConstraints.None;
            AAMRigid.angularVelocity = Vector3.zero;
            Vector3 LaunchPoint = EntityControl.transform.position + EntityControl.transform.TransformDirection(LocalLaunchPoint);
            transform.position = LaunchPoint;
            AAMRigid.position = LaunchPoint;
            TargetSAVControl = null;
            TargetEntityControl = null;
            hitwater = false;
            StartTrack = false;
            Exploding = false;
        }
        private void OnCollisionEnter(Collision other)
        {
            if (!Exploding)
            {
                if (IsOwner && other.gameObject)
                {
                    DirectHit = true;
                    SaccEntity HitVehicle = other.gameObject.GetComponent<SaccEntity>();
                    SaccTarget HitTarget = other.gameObject.GetComponent<SaccTarget>();
                    if (HitVehicle || HitTarget)
                    {
                        float Armor = 1;
                        bool ColliderHasArmorValue = false;
                        if (other.collider.transform.childCount > 0)
                        {
                            string pname = other.collider.transform.GetChild(0).name;
                            ColliderHasArmorValue = getArmorValue(pname, ref Armor);
                        }
                        if (HitVehicle)
                        {
                            float dmg = AAMDamage_AbsoluteMode ? AAMDamage : AAMDamage * (float)TargetSAVControl.GetProgramVariable("FullHealth");
                            if (!ColliderHasArmorValue)
                            {
                                Armor = HitVehicle.ArmorStrength;
                            }
                            dmg /= Armor;
                            if (dmg > HitVehicle.NoDamageBelow)
                                HitVehicle.WeaponDamageVehicle(dmg, EntityControl.gameObject, event_WeaponType);
                        }
                        else if (HitTarget)
                        {
                            float dmg = AAMDamage_AbsoluteMode ? AAMDamage : AAMDamage * (float)HitTarget.GetProgramVariable("FullHealth");
                            if (!ColliderHasArmorValue)
                            {
                                Armor = HitTarget.ArmorStrength;
                            }
                            dmg /= Armor;
                            if (dmg > HitTarget.NoDamageBelow)
                                HitTarget.WeaponDamageTarget(dmg, EntityControl.gameObject, event_WeaponType);
                        }
                    }
                }
                hitwater = false;
                Explode();
            }
        }
        bool getArmorValue(string name, ref float armor)
        {
            // Find the last colon in the string
            int index = name.LastIndexOf(':');
            if (index < 0 || index == name.Length - 1) // Check if colon exists and not at the end
            {
                return false;
            }
            string numberStr = name.Substring(index + 1); // Get substring after colon
            // Check if the remaining part is a valid number
            if (!float.TryParse(numberStr, out float parsedArmor))
            {
                return false;
            }
            // Only accept positive numbers
            if (parsedArmor <= 0f)
            {
                return false;
            }
            armor = parsedArmor;
            return true;
        }
        private void Explode()
        {
            if (AAMRigid)
            {
                AAMRigid.constraints = RigidbodyConstraints.FreezePosition;
                AAMRigid.velocity = Vector3.zero;
            }
            Exploding = true;
            TargetLost = true;
            if (hitwater && WaterExplosionSounds.Length > 0)
            {
                int rand = Random.Range(0, WaterExplosionSounds.Length);
                WaterExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
                WaterExplosionSounds[rand].Play();
            }
            else
            {
                if (ExplosionSounds.Length > 0)
                {
                    int rand = Random.Range(0, ExplosionSounds.Length);
                    ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
                    ExplosionSounds[rand].Play();
                }
            }
            if (MissileIncoming)
            {
                if ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger"))
                {
                    TargetSAVControl.SetProgramVariable(MissileTypes[MissileType], (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]) - 1);
                }
                TargetAnimator.SetInteger(MissileIncomingName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                MissileIncoming = false;
            }

            AAMCollider.enabled = false;
            float DamageDist = 999f;
            if (TargetEntityControl && (DirectHit || SplashHit))
            {
                TargetEntityControl.LastAttacker = EntityControl;
                DamageDist = Vector3.Distance(transform.position, ((Transform)TargetSAVControl.GetProgramVariable("CenterOfMass")).position) / ProximityExplodeDistance;
                if (IsOwner)
                {
                    if (DamageDist < 1 && !DirectHit)
                    {
                        float Armor = TargetEntityControl.ArmorStrength;
                        float dmg = ((AAMDamage_AbsoluteMode ? AAMDamage : AAMDamage * (float)SAVControl.GetProgramVariable("FullHealth")) * DamageDist) / Armor;
                        if (dmg > TargetEntityControl.NoDamageBelow)
                        { TargetEntityControl.WeaponDamageVehicle(dmg, EntityControl.gameObject, event_WeaponType); }
                    }
                }
            }
            MissileAnimator.SetTrigger("explode");
            MissileAnimator.SetBool("hitwater", hitwater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);
        }
    }
}