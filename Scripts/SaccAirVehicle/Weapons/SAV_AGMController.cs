
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_AGMController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour AGMLauncherControl;
        UdonSharpBehaviour SAVControl;
        [Tooltip("Missile will explode after this time")]
        public float MaxLifetime = 35;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        public float ExplosionLifeTime = 10;
        [Tooltip("AGM will fly straight for this many seconds before it starts homing in on target")]
        public float FlyStraightTime = .3f;
        [Tooltip("Play a random one of these explosion sounds")]
        public AudioSource[] ExplosionSounds;
        [Tooltip("Play a random one of these explosion sounds when hitting water")]
        public AudioSource[] WaterExplosionSounds;
        [Tooltip("Distance from plane to enable the missile's collider, to prevent missile from colliding with own plane")]
        public float ColliderActiveDistance = 30;
        [Tooltip("Max angle able to track target at")]
        public float LockAngle = 90;
        [Tooltip("Maximum speed missile can rotate")]
        public float RotSpeed = 15;
        [Range(1.01f, 2f)]
        [Tooltip("Amount the target direction vector is extended when calculating missile rotation. Lower number = more aggressive drifting missile, but more likely to oscilate")]
        public float TargetVectorExtension = 1.2f;
        [Tooltip("Strength of the forces applied to the sides of the missiles as it drifts through the air when it turns")]
        public float AirPhysicsStrength = 3f;
        [Tooltip("This velocity is added to the missile when it spawns.")]
        public Vector3 ThrowVelocity = new Vector3(0, 0, 0);
        [Tooltip("Enable this tickbox to make the ThrowVelocity vector local to the vehicle instead of the missile")]
        public bool ThrowSpaceVehicle = false;
        public float DirectHitDamage = 320;
        [Tooltip("event_WeaponType is sent with damage and kill events, but not used for anything in the base prefab.\n0=None/Suicide,1=Gun,2=AAM,3=AGM,4=Bomb,5=Rocket,6=Cannon,7=Laser,8=Beam,9=Torpedo,10=VLS,11=Javelin,12=Railgun, anything else is undefined (custom) 0-255")]
        [SerializeField] private byte event_WeaponType = 3;
        [Header("Knockback")]
        [SerializeField] float SplashRadius = 10;
        [Tooltip("Do not reduce knockback strength based on mass")]
        [SerializeField] bool KnockbackIgnoreMass = false;
        [SerializeField] float KnockbackStrength_rigidbody = 3750f;
        [SerializeField] float KnockbackStrength_players = 10f;
        [SerializeField] float KnockbackStrength_players_vert = 2f;
        [SerializeField] private bool ExpandingShockwave = false;
        [SerializeField] private float AGMDamage = 320;
        [SerializeField] private float ExpandingShockwave_Speed = 343f;
        [Tooltip("Sound to play when shockwave hits the player")]
        [SerializeField] AudioSource[] ShockwaveHitMe_Sound;
        [Tooltip("Maximum number of rigidboes that can be blasted, to save performance")]
        [SerializeField] private int Shockwave_max_targets = 30;
        [SerializeField] private Transform shockWaveSphere;
        private Animator MissileAnimator;
        [System.NonSerialized] public SaccEntity EntityControl;
        private bool StartTrack = false;
        private Transform TargetTransform;
        private Vector3 TargetOffset;
        private bool ColliderActive = false;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private bool IsOwner = false;
        private Collider AGMCollider;
        private Rigidbody AGMRigid;
        private Rigidbody VehicleRigid;
        private bool hitwater;
        private UdonSharpBehaviour DirectHitObjectScript = null;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private bool ColliderAlwaysActive;
        Vector3 LocalLaunchPoint;
        private void Initialize()
        {
            initialized = true;
            EntityControl = (SaccEntity)AGMLauncherControl.GetProgramVariable("EntityControl");
            SAVControl = (UdonSharpBehaviour)AGMLauncherControl.GetProgramVariable("SAVControl");
            AGMCollider = gameObject.GetComponent<Collider>();
            AGMRigid = gameObject.GetComponent<Rigidbody>();
            VehicleRigid = EntityControl.VehicleRigidbody;
            MissileAnimator = gameObject.GetComponent<Animator>();
            ColliderAlwaysActive = ColliderActiveDistance == 0;
            if (ExpandingShockwave)
            { ExplosionLifeTime = Mathf.Max(ExplosionLifeTime, SplashRadius / ExpandingShockwave_Speed); }
            HitRBs = new Rigidbody[Shockwave_max_targets];
            HitTargets = new SaccTarget[Shockwave_max_targets];
        }
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
            IsOwner = (bool)AGMLauncherControl.GetProgramVariable("IsOwner");
            if (ColliderAlwaysActive) { AGMCollider.enabled = true; ColliderActive = true; }
            else { AGMCollider.enabled = false; ColliderActive = false; }
            LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(transform.position - EntityControl.transform.position);
            AGMRigid.velocity += ThrowSpaceVehicle ? EntityControl.transform.TransformDirection(ThrowVelocity) : transform.TransformDirection(ThrowVelocity);
            TargetTransform = (Transform)AGMLauncherControl.GetProgramVariable("TrackedTransform");
            TargetOffset = (Vector3)AGMLauncherControl.GetProgramVariable("TrackedObjectOffset");
            if (MaxLifetime == 0)
            {
                if (!Exploding && gameObject.activeSelf)
                { hitwater = false; Explode(); }
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
                LifeTimeExplodesSent++;
                SendCustomEventDelayedSeconds(nameof(StartTracking), FlyStraightTime);

                if (ColliderAlwaysActive && !EntityControl.IsOwner && AGMRigid && !AGMRigid.isKinematic && SAVControl)
                {
                    // because non-owners update position of vehicle in Update() via SyncScript, it can clip into the projectile before next physics update
                    // So in the updates until then move projectile by vehiclespeed
                    ensureNoSelfCollision_time = Time.fixedTime;
                    ensureNoSelfCollision();
                }
            }
        }
        float ensureNoSelfCollision_time;
        public void ensureNoSelfCollision()
        {
            if (ensureNoSelfCollision_time != Time.fixedTime) return;

            transform.position += (Vector3)SAVControl.GetProgramVariable("CurrentVel") * Time.deltaTime;
            AGMRigid.position = transform.position;
            SendCustomEventDelayedFrames(nameof(ensureNoSelfCollision), 1);
        }
        void LateUpdate()
        {
            if (Exploding) return;
            Vector3 missileToTargetVector = TargetTransform.TransformPoint(TargetOffset) - transform.position;
            float DeltaTime = Time.deltaTime;
            if (!ColliderActive)
            {
                Vector3 LaunchPoint = (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position;
                if (Vector3.Distance(AGMRigid.position, LaunchPoint) > ColliderActiveDistance)
                {
                    AGMCollider.enabled = true;
                    ColliderActive = true;
                }
            }
            if (StartTrack && Vector3.Angle(transform.forward, missileToTargetVector) < LockAngle)
            {
                Vector3 TargetDirNormalized = missileToTargetVector.normalized * TargetVectorExtension;
                Vector3 MissileVelNormalized = AGMRigid.velocity.normalized;
                Vector3 MissileForward = transform.forward;
                Vector3 targetDirection = TargetDirNormalized - MissileVelNormalized;
                Vector3 RotationAxis = Vector3.Cross(MissileForward, targetDirection);
                float deltaAngle = Vector3.Angle(MissileForward, targetDirection);
                transform.Rotate(RotationAxis, Mathf.Min(RotSpeed * DeltaTime, deltaAngle), Space.World);
                AGMRigid.rotation = transform.rotation;
            }
        }
        void FixedUpdate()
        {
            float sidespeed = Vector3.Dot(AGMRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(AGMRigid.velocity, transform.up);
            AGMRigid.AddRelativeForce(new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, 0), ForceMode.Acceleration);
        }
        public void StartTracking()
        { StartTrack = true; }
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
        public void MoveBackToPool()
        {
            MissileAnimator.WriteDefaultValues();
            gameObject.SetActive(false);
            transform.SetParent(AGMLauncherControl.transform);
            AGMCollider.enabled = false;
            AGMRigid.constraints = RigidbodyConstraints.None;
            AGMRigid.angularVelocity = Vector3.zero;
            Vector3 LaunchPoint = EntityControl.transform.position + EntityControl.transform.TransformDirection(LocalLaunchPoint);
            transform.position = LaunchPoint;
            AGMRigid.position = LaunchPoint;
            hitwater = false;
            StartTrack = false;
            Exploding = false;
            DirectHitObjectScript = null;
            ShockwaveActive = false;
        }
        private void OnCollisionEnter(Collision other)
        {
            if (Exploding) return;
            // Ricochets could be added here
            if (IsOwner && other.gameObject)
            {
                SaccEntity HitVehicle = other.gameObject.GetComponent<SaccEntity>();
                SaccTarget HitTarget = other.gameObject.GetComponent<SaccTarget>();
                if (HitVehicle || HitTarget)
                {
                    float Armor = -1;
                    bool ColliderHasArmorValue = false;
                    if (other.collider.transform.childCount > 0)
                    {
                        string pname = other.collider.transform.GetChild(0).name;
                        ColliderHasArmorValue = getArmorValue(pname, ref Armor);
                    }

                    if (HitVehicle)
                    {
                        if (!ColliderHasArmorValue)
                        {
                            Armor = HitVehicle.ArmorStrength;
                        }
                        float dmg = AGMDamage / Armor;
                        if (dmg > HitVehicle.NoDamageBelow || dmg < 0)
                        {
                            HitVehicle.WeaponDamageVehicle(dmg, EntityControl.gameObject, event_WeaponType);
                            DirectHitObjectScript = HitVehicle;
                        }
                    }
                    else if (HitTarget)
                    {
                        if (!ColliderHasArmorValue)
                        {
                            Armor = HitTarget.ArmorStrength;
                        }
                        float dmg = AGMDamage / Armor;
                        if (dmg > HitTarget.NoDamageBelow || dmg < 0)
                        {
                            HitTarget.WeaponDamageTarget(dmg, EntityControl.gameObject, event_WeaponType);
                            DirectHitObjectScript = HitTarget;
                        }
                    }
                }
            }
            hitwater = false;
            Explode();
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
        private void Explode()
        {
            if (Exploding) return;
            if (AGMRigid)
            {
                AGMRigid.constraints = RigidbodyConstraints.FreezePosition;
                AGMRigid.velocity = Vector3.zero;
            }
            Exploding = true;
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
            AGMCollider.enabled = false;
            if (IsOwner)
            { MissileAnimator.SetTrigger("explodeowner"); }
            else { MissileAnimator.SetTrigger("explode"); }
            MissileAnimator.SetBool("hitwater", hitwater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);


            if (SplashRadius == 0) return;
            if (ExpandingShockwave)
            {
                _ExpandingShockwave_Speed = ExpandingShockwave_Speed;
            }
            else
            {
                _ExpandingShockwave_Speed = 0;
                CurrentShockwave = SplashRadius;
            }
            if (!ShockwaveActive)
            {
                if (shockWaveSphere) shockWaveSphere.gameObject.SetActive(true);
                ShockwaveActive = true;
                Shockwave();
            }
        }
        bool hitMAXTargets = false;
        float CurrentShockwave;
        float _ExpandingShockwave_Speed;
        bool ShockwaveActive;
        public void Shockwave()
        {
            CurrentShockwave = Mathf.Min(CurrentShockwave + (_ExpandingShockwave_Speed * Time.deltaTime), SplashRadius);
            if (shockWaveSphere) shockWaveSphere.localScale = Vector3.one * CurrentShockwave * 2;
            //rigidbodies
            int numHits = Physics.OverlapSphereNonAlloc(transform.position, CurrentShockwave, hitobjs);
            if (!hitMAXTargets)
            {
                for (int i = 0; i < numHits; i++)
                {
                    if (!hitobjs[i]) continue;
                    Rigidbody thisRB = hitobjs[i].attachedRigidbody;
                    if (thisRB)
                    {
                        bool gayflag = false;
                        for (int o = 0; o < numHitRBs; o++)
                        {
                            if (thisRB == HitRBs[o])
                            {
                                gayflag = true;
                                break;
                            }
                        }
                        if (gayflag) continue;

                        Vector3 explosionDirRB = thisRB.worldCenterOfMass - transform.position;

                        float DamageFalloff;
                        if (ExpandingShockwave)
                            DamageFalloff = 1 - (CurrentShockwave / SplashRadius);
                        else
                            DamageFalloff = 1 - (Mathf.Min(explosionDirRB.magnitude, SplashRadius) / SplashRadius);
                        if (DamageFalloff > 0)
                        {
                            if (!thisRB.isKinematic && Networking.IsOwner(thisRB.gameObject))
                                thisRB.AddForce(KnockbackStrength_rigidbody * DamageFalloff * explosionDirRB.normalized, KnockbackIgnoreMass ? ForceMode.VelocityChange : ForceMode.Impulse);
                            if (IsOwner)
                            {
                                SaccEntity hitEntity = thisRB.GetComponent<SaccEntity>();
                                if (hitEntity)
                                {
                                    if ((UdonSharpBehaviour)hitEntity != DirectHitObjectScript)
                                    {
                                        float SplashDamage = AGMDamage * DamageFalloff;
                                        if (SplashDamage > hitEntity.NoDamageBelow || SplashDamage < 0)
                                        {
                                            hitEntity.WeaponDamageVehicle(SplashDamage, EntityControl.gameObject, event_WeaponType);
                                        }
                                    }
                                }
                                else
                                {
                                    SaccTarget hitTarget = thisRB.GetComponent<SaccTarget>();
                                    if (hitTarget)
                                    {
                                        if ((UdonSharpBehaviour)hitTarget != DirectHitObjectScript)
                                        {
                                            float SplashDamage = AGMDamage * DamageFalloff;
                                            if (SplashDamage > hitTarget.NoDamageBelow || SplashDamage < 0)
                                            {
                                                hitTarget.WeaponDamageTarget(SplashDamage, EntityControl.gameObject, event_WeaponType);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        HitRBs[numHitRBs] = thisRB;
                        numHitRBs++;
                        numHitObjects++;
                        if (numHitObjects == Shockwave_max_targets) { hitMAXTargets = true; break; }
                    }
                    else
                    {
                        if (IsOwner)
                        {
                            Vector3 explosionDirTarget = hitobjs[i].transform.position - transform.position;
                            SaccTarget thisTarget = hitobjs[i].GetComponent<SaccTarget>();
                            if (thisTarget)
                            {
                                bool gayflag = false;
                                for (int o = 0; o < numHitTargets; o++)
                                {
                                    if (thisTarget == HitTargets[o])
                                    {
                                        gayflag = true;
                                        break;
                                    }
                                }
                                if (gayflag) continue;
                                if ((UdonSharpBehaviour)thisTarget != DirectHitObjectScript)
                                {
                                    float DamageFalloff;
                                    if (ExpandingShockwave)
                                        DamageFalloff = 1 - (CurrentShockwave / SplashRadius);
                                    else
                                        DamageFalloff = 1 - (Mathf.Min(explosionDirTarget.magnitude, SplashRadius) / SplashRadius);
                                    float SplashDamage = AGMDamage * DamageFalloff;
                                    if (SplashDamage > thisTarget.NoDamageBelow || SplashDamage < 0)
                                    {
                                        thisTarget.WeaponDamageTarget(SplashDamage, EntityControl.gameObject, event_WeaponType);
                                    }
                                }
                                HitTargets[numHitTargets] = thisTarget;
                                numHitTargets++;
                                numHitObjects++;
                                if (numHitObjects == Shockwave_max_targets) { hitMAXTargets = true; break; }
                            }
                        }
                    }
                }
            }
            //players
            if (!ShockwaveHitMe)
            {
                if (Vector3.Distance(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, transform.position) < CurrentShockwave)
                {
                    Vector3 explosionDir = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - transform.position;
                    float knockback = (SplashRadius - explosionDir.magnitude) / SplashRadius;
                    if (knockback > 0)
                    {
                        Networking.LocalPlayer.SetVelocity(Networking.LocalPlayer.GetVelocity() + (KnockbackStrength_players * knockback * explosionDir.normalized) +
                        KnockbackStrength_players_vert * knockback * Vector3.up
                        );
                    }
                    if (ShockwaveHitMe_Sound.Length > 0)
                    {
                        int rand = Random.Range(0, ShockwaveHitMe_Sound.Length);
                        ShockwaveHitMe_Sound[rand].Play();
                    }
                    ShockwaveHitMe = true;
                }
            }

            if ((CurrentShockwave < SplashRadius && ExpandingShockwave) && ShockwaveActive)
            {
                SendCustomEventDelayedFrames(nameof(Shockwave), 1);
            }
            else
            {
                ShockwaveActive = false;
                CurrentShockwave = 0;
                ShockwaveHitMe = false;
                hitMAXTargets = false;
                numHitRBs = 0;
                numHitTargets = 0;
                numHitObjects = 0;
                HitRBs = new Rigidbody[Shockwave_max_targets];
                HitTargets = new SaccTarget[Shockwave_max_targets];
                if (shockWaveSphere) SendCustomEventDelayedFrames(nameof(disableShockWaveSphere), 1);// so it's visible for at least 1 frame and it's max size is visible
            }
        }
        public void disableShockWaveSphere() { shockWaveSphere.gameObject.SetActive(false); }
        bool ShockwaveHitMe;
        uint numHitObjects; // RBs+Targets
        Collider[] hitobjs = new Collider[100];
        uint numHitRBs;
        Rigidbody[] HitRBs;
        uint numHitTargets;
        SaccTarget[] HitTargets;
    }
}