
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_BombController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour BombLauncherControl;
        [Tooltip("Bomb will explode after this time")]
        [SerializeField] private float MaxLifetime = 40;
        [Tooltip("Maximum liftime of bomb is randomized by +- this many seconds on appearance")]
        [SerializeField] private float MaxLifetimeRadnomization = 2f;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        [SerializeField] private float ExplosionLifeTime = 10;
        [Tooltip("Play a random one of these explosion sounds")]
        [SerializeField] private AudioSource[] ExplosionSounds;
        [Tooltip("Play a random one of these explosion sounds when hitting water")]
        [SerializeField] private AudioSource[] WaterExplosionSounds;
        [Tooltip("Bomb flies forward with this much extra speed, can be used to make guns/shells")]
        [SerializeField] private float LaunchSpeed = 0;
        [Tooltip("Spawn bomb at a random angle up to this number")]
        [SerializeField] private float AngleRandomization = 1;
        [Tooltip("Distance from plane to enable the missile's collider, to prevent bomb from colliding with own plane")]
        [SerializeField] private float ColliderActiveDistance = 30;
        [Tooltip("How much the bomb's nose is pushed towards direction of movement")]
        [SerializeField] private float StraightenFactor = .1f;
        [Tooltip("Amount of drag bomb has when moving horizontally/vertically")]
        [SerializeField] private float AirPhysicsStrength = .1f;
        [Tooltip("For colliders using the armor system (Tanks), final damage = base damage * 2^(ArmorPenetrationLevel - ColliderArmorLevel)")]
        [Range(0, 14)]
        public int ArmorPenetrationLevel = 5;
        [Header("Torpedo mode settings")]
        [SerializeField] private bool IsTorpedo;
        [SerializeField] private float TorpedoSpeed = 60;
        [SerializeField] private ParticleSystem WakeParticle;
        private ParticleSystem.EmissionModule WakeParticle_EM;
        [SerializeField] private float TorpedoDepth = -.25f;
        [SerializeField] private ParticleSystem[] DisableInWater_ParticleEmission;
        private ParticleSystem.EmissionModule[] DisableInWater_ParticleEmission_EM;
        [SerializeField] private TrailRenderer[] DisableInWater_TrailEmission;
        [SerializeField] private GameObject[] DisableInWater;
        [Header("Knockback")]
        [SerializeField] float KnockbackRadius = 0;
        [SerializeField] bool KnobckbackModeAcceleration = false;
        [SerializeField] float KnockbackStrength_rigidbody = 150f;
        [SerializeField] float KnockbackStrength_players = 1.5f;
        private Transform WakeParticle_Trans;
        private float TorpedoHeight;
        private Quaternion TorpedoRot;
        private Animator BombAnimator;
        [System.NonSerialized] public SaccEntity EntityControl;
        private Rigidbody VehicleRigid;
        private Rigidbody BombRigid;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private bool ColliderActive = false;
        private Collider BombCollider;
        private bool UnderWater;
        private bool hitwater;
        private bool IsOwner;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private bool ColliderAlwaysActive;
        private float DragStart;
        Vector3 LocalLaunchPoint;
        private void Initialize()
        {
            initialized = true;
            EntityControl = (SaccEntity)BombLauncherControl.GetProgramVariable("EntityControl");
            BombCollider = GetComponent<Collider>();
            BombRigid = GetComponent<Rigidbody>();
            VehicleRigid = EntityControl.VehicleRigidbody;
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
            BombAnimator = GetComponent<Animator>();
            ColliderAlwaysActive = ColliderActiveDistance == 0;
            DragStart = BombRigid.drag;
            if (WakeParticle)
            {
                WakeParticle_Trans = WakeParticle.transform;
                WakeParticle_EM = WakeParticle.emission;
            }
            DisableInWater_ParticleEmission_EM = new ParticleSystem.EmissionModule[DisableInWater_ParticleEmission.Length];
            for (int i = 0; i < DisableInWater_ParticleEmission.Length; i++)
            {
                DisableInWater_ParticleEmission_EM[i] = DisableInWater_ParticleEmission[i].emission;
            }
        }
        public void AddLaunchSpeed()
        {
            BombRigid.velocity += transform.forward * LaunchSpeed;
        }
        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(transform.position - EntityControl.transform.position);
            if (ColliderAlwaysActive) { BombCollider.enabled = true; ColliderActive = true; }
            else { BombCollider.enabled = false; ColliderActive = false; }
            if (EntityControl && EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner"); }
            SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime + Random.Range(-MaxLifetimeRadnomization, MaxLifetimeRadnomization));
            LifeTimeExplodesSent++;
            SendCustomEventDelayedFrames(nameof(AddLaunchSpeed), 1);//doesn't work if done this frame
        }
        void LateUpdate()
        {
            if (Exploding) return;
            if (!ColliderActive)
            {
                Vector3 LaunchPoint = (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position;
                if (Vector3.Distance(BombRigid.position, LaunchPoint) > ColliderActiveDistance)
                {
                    BombCollider.enabled = true;
                    ColliderActive = true;
                }
            }
        }
        void FixedUpdate()
        {
            float sidespeed = Vector3.Dot(BombRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(BombRigid.velocity, transform.up);
            BombRigid.AddRelativeTorque(new Vector3(-downspeed, sidespeed, 0) * StraightenFactor, ForceMode.Acceleration);
            BombRigid.AddRelativeForce(new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, 0), ForceMode.Acceleration);
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
        public void MoveBackToPool()
        {
            BombAnimator.WriteDefaultValues();
            gameObject.SetActive(false);
            transform.SetParent(BombLauncherControl.transform);
            BombCollider.enabled = false;
            BombRigid.constraints = RigidbodyConstraints.None;
            BombRigid.angularVelocity = Vector3.zero;
            Vector3 LaunchPoint = EntityControl.transform.position + EntityControl.transform.TransformDirection(LocalLaunchPoint);
            transform.position = LaunchPoint;
            BombRigid.position = LaunchPoint;
            Exploding = false;
            UnderWater = false;
            BombRigid.drag = DragStart;
            for (int i = 0; i < DisableInWater.Length; i++) { DisableInWater[i].SetActive(true); }
            for (int i = 0; i < DisableInWater_ParticleEmission_EM.Length; i++) { DisableInWater_ParticleEmission_EM[i].enabled = true; }
            for (int i = 0; i < DisableInWater_TrailEmission.Length; i++) { DisableInWater_TrailEmission[i].emitting = true; }
        }
        private void OnCollisionEnter(Collision other)
        {
            if (Exploding) return;
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
                    bool DoDamage = true;
                    dmgLvl = dmgLvl - Armor;
                    if (dmgLvl > 0) DoDamage = true;

                    if (DoDamage) HitVehicle.WeaponDamageVehicle(dmgLvl, gameObject);
                }
            }
            hitwater = false;
            Explode();
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
                if (hitwater) { return; }
                if (!IsTorpedo)
                {
                    if (!Exploding)
                    {
                        hitwater = true;
                        Explode();
                    }
                }
                else
                {
                    for (int i = 0; i < DisableInWater.Length; i++) { DisableInWater[i].SetActive(false); }
                    for (int i = 0; i < DisableInWater_ParticleEmission_EM.Length; i++) { DisableInWater_ParticleEmission_EM[i].enabled = false; }
                    for (int i = 0; i < DisableInWater_TrailEmission.Length; i++) { DisableInWater_TrailEmission[i].emitting = false; }
                    // When a torpedo hits the water it freezes it's height and rotation
                    // removes all drag and just sets its speed once and goes straight.
                    UnderWater = true;
                    BombRigid.angularVelocity = Vector3.zero;
                    BombRigid.constraints = (RigidbodyConstraints)116; // Freeze all rotation and Y position

                    Vector3 bvel = BombRigid.velocity;
                    bvel.y = 0;

                    Vector3 brot = transform.eulerAngles;
                    brot.x = 0;
                    brot.z = 0;
                    if (brot.y > 180) { brot.y -= 360; }
                    Quaternion newrot = Quaternion.Euler(brot);
                    TorpedoRot = newrot;
                    transform.rotation = newrot;
                    BombRigid.rotation = newrot;

                    //Find the water height
                    RaycastHit WH;
                    if (Physics.Raycast(transform.position + (Vector3.up * 100), -Vector3.up, out WH, 150, 16/* Water */, QueryTriggerInteraction.Collide))
                    { TorpedoHeight = WH.point.y + TorpedoDepth; }
                    else
                    { TorpedoHeight = transform.position.y; }

                    if (WakeParticle)
                    {
                        WakeParticle_EM.enabled = true;
                        WakeParticle.transform.position = WH.point + Vector3.up * 0.1f;
                    }
                    Vector3 bpos = transform.position;
                    bpos.y = TorpedoHeight;
                    transform.position = bpos;
                    BombRigid.velocity = transform.forward * TorpedoSpeed;
                    BombRigid.drag = 0;
                }
            }
        }
        private void Explode()
        {
            if (BombRigid)
            {
                BombRigid.constraints = RigidbodyConstraints.FreezePosition;
                BombRigid.velocity = Vector3.zero;
            }
            Exploding = true;
            if ((hitwater || UnderWater) && WaterExplosionSounds.Length > 0)
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
            BombCollider.enabled = false;
            if (WakeParticle) WakeParticle_EM.enabled = false;
            if (IsOwner)
            { BombAnimator.SetTrigger("explodeowner"); }
            else { BombAnimator.SetTrigger("explode"); }
            BombAnimator.SetBool("hitwater", hitwater || UnderWater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);


            if (KnockbackRadius == 0) return;
            //rigidbodies
            int numHits = Physics.OverlapSphereNonAlloc(transform.position, KnockbackRadius, hitobjs);
            int numRBs = 0;
            for (int i = 0; i < numHits; i++)
            {
                if (!hitobjs[i]) continue;
                Rigidbody thisRB = hitobjs[i].attachedRigidbody;
                if (!thisRB) continue;
                bool gayflag = false;
                for (int o = 0; o < numRBs; o++)
                {
                    if (thisRB == HitRBs[o])
                    {
                        gayflag = true;
                        break;
                    }
                }
                if (gayflag) continue;
                HitRBs[numRBs] = thisRB;
                numRBs++;
                if (numRBs == 30) break;
            }
            for (int i = 0; i < numRBs; i++)
            {
                if (HitRBs[i].isKinematic) continue;
                Vector3 explosionDirRB = HitRBs[i].worldCenterOfMass - transform.position;
                float knockbackRB = KnockbackRadius - explosionDirRB.magnitude;
                if (knockbackRB > 0)
                {
                    HitRBs[i].AddForce(KnockbackStrength_rigidbody * knockbackRB * explosionDirRB.normalized, KnobckbackModeAcceleration ? ForceMode.VelocityChange : ForceMode.Impulse);
                    SaccEntity hitEntity = HitRBs[i].GetComponent<SaccEntity>();
                    if (hitEntity && hitEntity.IsOwner)
                    {
                        hitEntity.SendEventToExtensions("SFEXT_L_WakeUp");
                    }
                }
            }
            //players
            Vector3 explosionDir = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - transform.position;
            float knockback = KnockbackRadius - explosionDir.magnitude;
            if (knockback > 0)
            {
                Networking.LocalPlayer.SetVelocity(Networking.LocalPlayer.GetVelocity() + KnockbackStrength_players * knockback * explosionDir.normalized);
            }
        }
        Collider[] hitobjs = new Collider[100];
        Rigidbody[] HitRBs = new Rigidbody[30];
    }
}