
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
        [Tooltip("Missile will explode after this time")]
        public float MaxLifetime = 12;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        public float ExplosionLifeTime = 10;
        [Tooltip("Strength of the effect of countermeasures on the missile (ignore 'Flare', it's the effect of whatever countermeasure type this missile is effected by")]
        public float FlareEffect = 10;
        [Tooltip("For simulating FOX-1/3 missiles")]
        public bool RequireParentLock = false;
        [Tooltip("For simulating FOX-3. Missile will not require parent vehicle lock after it is closer to target than this distance. Unlike a real FOX-3, it will only chase it's original target in pitbull mode. Meters. Set to 0 for FOX-1")]
        public float PitBullDistance = 0;
        [Range(0, 180f)]
        [Tooltip("If the missile and target vehicle are facing towards each other, multiply rotation speed by HighAspectRotSpeedMulti with this nose angle (facing perfectly towards each other = 0 degrees, which is the same as disabled) Set 0 for any non-heatseeker missiles")]
        public float HighAspectTrackAngle = 60;
        [Tooltip("See above")]
        public float HighAspectRotSpeedMulti = .5f;
        [Tooltip("Send the target plane's animator an integer +1 whilst this missile is flying towards it")]
        public bool SendAnimInt = true;
        [Tooltip("Name of animator integer to +1 on the target plane while chasing it")]
        public string AnimINTName = "missilesincoming";
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
        private string[] MissileTypes = { "MissilesIncomingHeat", "MissilesIncomingRadar", "MissilesIncomingOther" };//names of variables in SaccAirVehicle
        private string[] CMTypes = { "NumActiveFlares", "NumActiveChaff", "NumActiveOtherCM" };//names of variables in SaccAirVehicle
        private SaccEntity EntityControl;
        private int MissileType = 1;
        private UdonSharpBehaviour TargetSAVControl;
        private Animator TargetAnimator;
        SaccEntity TargetEntityControl;
        private bool LockHack = true;
        private bool PitBull = false;
        private bool DoPitBull = false;
        private Transform Target;
        private bool ColliderActive = false;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private CapsuleCollider AAMCollider;
        private bool MissileIncoming = false;
        private Rigidbody MissileRigid;
        private float TargDistlastframe = 999999999;
        private bool TargetLost = false;
        private float UnlockTimer;
        private float TargetABPoint;
        private float TargetThrottleNormalizer;
        Vector3 TargetPosLastFrame;

        private Transform VehicleCenterOfMass;
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
        private ConstantForce MissileConstant;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private GameObject PitBullIndicator;
        private Animator MissileAnimator;
        GameObject[] AAMTargets;
        void Initialize()
        {
            EntityControl = (SaccEntity)AAMLauncherControl.GetProgramVariable("EntityControl");
            //whatever script is launching the missiles must contain all of these variables
            InEditor = (bool)AAMLauncherControl.GetProgramVariable("InEditor");
            VehicleCenterOfMass = EntityControl.CenterOfMass;
            MissileAnimator = GetComponent<Animator>();
            MissileConstant = GetComponent<ConstantForce>();
            MissileRigid = GetComponent<Rigidbody>();
            AAMCollider = GetComponent<CapsuleCollider>();
            MissileType = (int)AAMLauncherControl.GetProgramVariable("MissileType");
            PitBullIndicator = (GameObject)AAMLauncherControl.GetProgramVariable("PitBullIndicator");

            DoPitBull = PitBullDistance > 0f;
            NotchHorizonDot = 1 - Mathf.Cos(NotchHorizon * Mathf.Deg2Rad);//angle as dot product
            NotchLimitDot = 1 - Mathf.Cos(NotchAngle * Mathf.Deg2Rad);
            HighAspectTrack = Mathf.Cos(HighAspectTrackAngle * Mathf.Deg2Rad);
        }
        public void StartTracking()
        {
            StartTrack = true;
        }
        public void ThrowMissile()
        {
            MissileRigid.velocity = MissileRigid.velocity + (ThrowSpaceVehicle ? EntityControl.transform.TransformDirection(ThrowVelocity) : transform.TransformDirection(ThrowVelocity));
        }
        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            if (EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)AAMLauncherControl.GetProgramVariable("IsOwner"); }
            int aamtarg = (int)AAMLauncherControl.GetProgramVariable("AAMTarget");
            AAMTargets = (GameObject[])AAMLauncherControl.GetProgramVariable("AAMTargets");
            Target = AAMTargets[aamtarg].transform;
            SendCustomEventDelayedFrames(nameof(ThrowMissile), 1);//doesn't work if done this frame

            //FixedUpdate runs one time after MoveBackToPool so these must be here
            ColliderActive = false;
            MissileConstant.relativeTorque = Vector3.zero;
            MissileConstant.relativeForce = Vector3.zero;
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
                        if (SendAnimInt && ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger")))
                        {
                            TargetSAVControl.SetProgramVariable(MissileTypes[MissileType], (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]) + 1);
                            MissileIncoming = true;
                        }
                        TargetEntityControl = (SaccEntity)TargetSAVControl.GetProgramVariable("EntityControl");
                        TargetAnimator = (Animator)TargetSAVControl.GetProgramVariable("VehicleAnimator");
                        TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                        TargetABPoint = (float)TargetSAVControl.GetProgramVariable("ThrottleAfterburnerPoint");
                        TargetThrottleNormalizer = 1 / TargetABPoint;
                    }
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
        void FixedUpdate()
        {
            float sidespeed = Vector3.Dot(MissileRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(MissileRigid.velocity, transform.up);
            float ConstantRelativeForce = MissileConstant.relativeForce.z;
            Vector3 NewConstantRelativeForce = new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, ConstantRelativeForce);
            MissileConstant.relativeForce = NewConstantRelativeForce;
            float DeltaTime = Time.fixedDeltaTime;
            if (!ColliderActive && Initialized)
            {
                if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
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
                Vector3 Targetmovedir = (TargetPos - TargetPosLastFrame) / DeltaTime;
                TargetPosLastFrame = TargetPos;
                Vector3 MissileToTargetVector = (TargetPos - Position).normalized;
                if (TargetSAVControl)
                {
                    MissileToTargetVector = (TargetPos - transform.position).normalized;
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
                        ((RequireParentLock && !PitBull) && (Target.gameObject != AAMTargets[(int)AAMLauncherControl.GetProgramVariable("AAMTarget")] || !AAMLauncherControl.gameObject.activeSelf))
                        ;
                    AspectTrack = Vector3.Dot(MissileToTargetVector, -TargetEntityControl.transform.forward) > HighAspectTrack ? HighAspectRotSpeedMulti : 1;
                    EngineTrack = Mathf.Max((float)TargetSAVControl.GetProgramVariable("EngineOutput") * TargetThrottleNormalizer, TargetMinThrottleTrack);//Track target more weakly the lower their throttle
                }
                else
                {
                    EngineTrack = 1;
                    AspectTrack = 1;
                    Dumb = //FOX-1
                        ((RequireParentLock && !PitBull) && (Target.gameObject != AAMTargets[(int)AAMLauncherControl.GetProgramVariable("AAMTarget")] || !AAMLauncherControl.gameObject.activeSelf));
                }
                if (EngineTrack > 1) { EngineTrack = AfterBurnerTrackMulti; }//if AB on, faster rotation
                if (Target.gameObject.activeInHierarchy && UnlockTimer < UnlockTime)
                {
                    if (!Dumb && Vector3.Dot(MissileToTargetVector, MissileRigid.velocity) > 0 || LockHack)
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
                        Vector3 MissileVelNormalized = MissileRigid.velocity.normalized;
                        Vector3 MissileForward = transform.forward;
                        Vector3 targetDirection = TargetDirNormalized - MissileVelNormalized;
                        Vector3 RotationAxis = Vector3.Cross(MissileForward, targetDirection);
                        float deltaAngle = Vector3.Angle(MissileForward, targetDirection);
                        transform.Rotate(RotationAxis, Mathf.Min(RotSpeed * EngineTrack * AspectTrack * DeltaTime, deltaAngle), Space.World);
                    }
                    else
                    {
                        if (TargetDistance < ProximityExplodeDistance)//missile flew past the target, but is within proximity explode range?
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
                            TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                        }
                        MissileIncoming = false;
                    }
                }
                TargDistlastframe = TargetDistance;
            }
        }
        public void DisableLockHack()
        { LockHack = false; }
        public void DisablePitBullIndicator()
        { PitBullIndicator.SetActive(false); }
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
            ColliderActive = false;
            MissileConstant.relativeTorque = Vector3.zero;
            MissileConstant.relativeForce = Vector3.zero;
            MissileRigid.constraints = RigidbodyConstraints.None;
            MissileRigid.angularVelocity = Vector3.zero;
            transform.localPosition = Vector3.zero;
            TargetSAVControl = null;
            TargetEntityControl = null;
            StartTrack = false;
            Exploding = false;
        }
        private void OnCollisionEnter(Collision other)
        {
            if (!Exploding)
            {
                if (IsOwner)
                {
                    DirectHit = true;
                    SaccEntity TargetEntity = other.gameObject.GetComponent<SaccEntity>();
                    if (TargetEntity)
                    {
                        TargetEntity.SendEventToExtensions("SFEXT_L_MissileHit100");
                        //Debug.Log("DIRECTHIT");
                    }
                }
                hitwater = false;
                Explode();
            }
        }
        private void Explode()
        {
            if (MissileRigid)
            {
                MissileRigid.constraints = RigidbodyConstraints.FreezePosition;
                MissileRigid.velocity = Vector3.zero;
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
                TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable(MissileTypes[MissileType]));
                MissileIncoming = false;
            }

            AAMCollider.enabled = false;
            float DamageDist = 999f;
            if (TargetEntityControl && (DirectHit || SplashHit))
            {
                TargetEntityControl.LastAttacker = EntityControl;
                TargetSAVControl.SetProgramVariable("LastHitTime", Time.time);
                DamageDist = Vector3.Distance(transform.position, ((Transform)TargetSAVControl.GetProgramVariable("CenterOfMass")).position) / ProximityExplodeDistance;
                if (IsOwner)
                {
                    if (DamageDist < 1 && !DirectHit)
                    {
                        //Debug.Log(string.Concat("TARGETDIST: ", Vector3.Distance(transform.position, ((Transform)TargetSAVControl.GetProgramVariable("CenterOfMass")).position).ToString()));
                        if (DamageDist > .66666f)
                        {
                            TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit25");
                        }
                        else if (DamageDist > .33333f)
                        {
                            TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit50");
                        }
                        else
                        {
                            TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit75");
                        }
                    }
                }
            }
            MissileAnimator.SetTrigger("explode");
            MissileAnimator.SetBool("hitwater", hitwater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);
        }
    }
}