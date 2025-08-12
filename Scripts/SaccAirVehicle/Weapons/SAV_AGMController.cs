
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
        [Tooltip("For colliders using the armor system (Tanks), final damage = base damage * 2^(ArmorPenetrationLevel - ColliderArmorLevel)")]
        [Range(0, 14)]
        public int ArmorPenetrationLevel = 5;
        [Header("Knockback")]
        [SerializeField] float KnockbackRadius = 0;
        [SerializeField] bool KnockbackModeAcceleration = false;
        [SerializeField] float KnockbackStrength_rigidbody = 3750f;
        [SerializeField] float KnockbackStrength_players = 10f;
        [SerializeField] float KnockbackStrength_players_vert = 2f;
        [SerializeField] private bool ExpandingShockwave = false;
        [Tooltip("Set damage level using the bullet damage system, (-9 - 14)")]
        [SerializeField] private int Shockwave_damage_level = -999;
        [SerializeField] private float ExpandingShockwave_Speed = 343f;
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
            { ExplosionLifeTime = Mathf.Max(ExplosionLifeTime, KnockbackRadius / ExpandingShockwave_Speed); }
        }
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
            if (EntityControl.InEditor) { IsOwner = true; }
            else { IsOwner = (bool)AGMLauncherControl.GetProgramVariable("IsOwner"); }
            if (ColliderAlwaysActive) { AGMCollider.enabled = true; ColliderActive = true; }
            else { AGMCollider.enabled = false; ColliderActive = false; }
            LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(transform.position - EntityControl.transform.position);
            AGMRigid.velocity += ThrowSpaceVehicle ? EntityControl.transform.TransformDirection(ThrowVelocity) : transform.TransformDirection(ThrowVelocity);
            TargetTransform = (Transform)AGMLauncherControl.GetProgramVariable("TrackedTransform");
            TargetOffset = (Vector3)AGMLauncherControl.GetProgramVariable("TrackedObjectOffset");
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
            StartTrack = false;
            Exploding = false;
        }
        private void OnCollisionEnter(Collision other)
        {
            // Ricochets could be added here
            if (IsOwner && other.gameObject)
            {
                SaccEntity HitVehicle = other.gameObject.GetComponent<SaccEntity>();
                if (HitVehicle)
                {
                    int dmgLvl = ArmorPenetrationLevel;

                    int Armor = 0;
                    if (other.collider.transform.childCount > 0)
                    {
                        string pname = other.collider.transform.GetChild(0).name;
                        getArmorValue(pname, ref Armor);
                    }
                    bool DoDamage = false;
                    dmgLvl = dmgLvl - Armor;
                    if (dmgLvl > 0) DoDamage = true;
                    if (DoDamage) HitVehicle.WeaponDamageVehicle(dmgLvl, gameObject);
                }
            }
            if (!Exploding)
            {
                hitwater = false; Explode();
            }
        }
        void getArmorValue(string name, ref int armor)
        {
            int index = name.LastIndexOf(':');
            if (index > -1)
            {
                name = name.Substring(index);
                if (name.Length == 3)
                {
                    if (name[1] >= '0' && name[1] <= '9')
                    {
                        if (name[2] >= '0' && name[2] <= '9')
                        {
                            armor = 10 * (name[1] - 48);
                            armor += name[2] - 48;
                        }
                    }
                }
            }
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


            if (KnockbackRadius == 0) return;
            if (ExpandingShockwave)
            {
                _ExpandingShockwave_Speed = ExpandingShockwave_Speed;
            }
            else
            {
                _ExpandingShockwave_Speed = 0;
                CurrentShockwave = KnockbackRadius;
            }
            if (shockWaveSphere) shockWaveSphere.gameObject.SetActive(true);
            Shockwave();
        }
        float CurrentShockwave;
        float _ExpandingShockwave_Speed;
        public void Shockwave()
        {
            CurrentShockwave += ExpandingShockwave_Speed * Time.deltaTime;
            if (shockWaveSphere) shockWaveSphere.localScale = Vector3.one * CurrentShockwave * 2;
            //rigidbodies
            int numHits = Physics.OverlapSphereNonAlloc(transform.position, CurrentShockwave, hitobjs);
            for (int i = 0; i < numHits; i++)
            {
                if (!hitobjs[i]) continue;
                Rigidbody thisRB = hitobjs[i].attachedRigidbody;
                if (!thisRB) continue;
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
                HitRBs[numHitRBs] = thisRB;
                numHitRBs++;
                if (numHitRBs == 30) break;


                if (thisRB.isKinematic) continue;
                Vector3 explosionDirRB = thisRB.worldCenterOfMass - transform.position;
                float knockbackRB = (KnockbackRadius - explosionDirRB.magnitude) / KnockbackRadius;
                if (knockbackRB > 0)
                {
                    thisRB.AddForce(KnockbackStrength_rigidbody * knockbackRB * explosionDirRB.normalized, KnockbackModeAcceleration ? ForceMode.VelocityChange : ForceMode.Impulse);
                    SaccEntity hitEntity = thisRB.GetComponent<SaccEntity>();
                    if (hitEntity && hitEntity.IsOwner)
                    {
                        hitEntity.SendEventToExtensions("SFEXT_L_WakeUp");
                        if (Shockwave_damage_level > -10)
                        {
                            hitEntity.SendDamageEvent(Shockwave_damage_level);
                        }
                    }
                }
            }
            if (!ShockwaveHitMe)
            {
                if (Vector3.Distance(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, transform.position) < CurrentShockwave)
                {
                    //players
                    Vector3 explosionDir = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - transform.position;
                    float knockback = (KnockbackRadius - explosionDir.magnitude) / KnockbackRadius;
                    if (knockback > 0)
                    {
                        Networking.LocalPlayer.SetVelocity(Networking.LocalPlayer.GetVelocity() + (KnockbackStrength_players * knockback * explosionDir.normalized) +
                        KnockbackStrength_players_vert * knockback * Vector3.up
                        );
                    }
                    ShockwaveHitMe = true;
                }
            }

            if (CurrentShockwave < KnockbackRadius && ExpandingShockwave)
            {
                SendCustomEventDelayedFrames(nameof(Shockwave), 1);
            }
            else
            {
                CurrentShockwave = 0;
                ShockwaveHitMe = false;
                numHitRBs = 0;
                Rigidbody[] HitRBs = new Rigidbody[30];
                if (shockWaveSphere) shockWaveSphere.gameObject.SetActive(false);
            }
        }
        bool ShockwaveHitMe;
        Collider[] hitobjs = new Collider[100];
        uint numHitRBs;
        Rigidbody[] HitRBs = new Rigidbody[30];
    }
}