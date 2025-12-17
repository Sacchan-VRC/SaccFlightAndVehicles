
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_BombController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour BombLauncherControl;
        UdonSharpBehaviour SAVControl;
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
        [SerializeField] private Transform PassBySound;
        [SerializeField] private float PassBySound_distance = 40f;
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
        public float BombDamage = 320;
        [Tooltip("event_WeaponType is sent with damage and kill events, but not used for anything in the base prefab.\n0=None/Suicide,1=Gun,2=AAM,3=AGM,4=Bomb,5=Rocket,6=Cannon,7=Laser,8=Beam,9=Torpedo,10=VLS,11=Javelin,12=Railgun, anything else is undefined (custom) 0-255")]
        [SerializeField] private byte event_WeaponType = 4;
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
        [Header("Knockback & Splash damage")]
        [SerializeField] float SplashRadius = 10;
        [Tooltip("Do not reduce knockback strength based on mass")]
        [SerializeField] bool KnockbackIgnoreMass = false;
        [SerializeField] float KnockbackStrength_rigidbody = 3750f;
        [SerializeField] float KnockbackStrength_players = 10f;
        [SerializeField] float KnockbackStrength_players_vert = 2f;
        [SerializeField] private bool ExpandingShockwave = false;
        [SerializeField] private float ExpandingShockwave_Speed = 343f;
        [Tooltip("Sound to play when shockwave hits the player")]
        [SerializeField] AudioSource[] ShockwaveHitMe_Sound;
        [Tooltip("Maximum number of rigidboes that can be blasted, to save performance")]
        [SerializeField] private int Shockwave_max_targets = 30;
        [SerializeField] private Transform shockWaveSphere;
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
        private UdonSharpBehaviour DirectHitObjectScript = null;
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
            SAVControl = (UdonSharpBehaviour)BombLauncherControl.GetProgramVariable("SAVControl");
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
            if (ExpandingShockwave)
            { ExplosionLifeTime = Mathf.Max(ExplosionLifeTime, SplashRadius / ExpandingShockwave_Speed); }
            HitRBs = new Rigidbody[Shockwave_max_targets];
            HitTargets = new SaccTarget[Shockwave_max_targets];
        }
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
            Exploding = hitwater = false;
            if (ColliderAlwaysActive) { BombCollider.enabled = true; ColliderActive = true; }
            else { BombCollider.enabled = false; ColliderActive = false; }
            BombRigid.velocity += transform.forward * LaunchSpeed;
            LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(transform.position - EntityControl.transform.position);
            IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner");
            if (MaxLifetime == 0)
            {
                if (!Exploding && gameObject.activeSelf)
                { hitwater = false; Explode(); }
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime + Random.Range(-MaxLifetimeRadnomization, MaxLifetimeRadnomization));
                LifeTimeExplodesSent++;
                float forwardDist = Vector3.Dot(transform.forward, Networking.LocalPlayer.GetPosition() - transform.position);
                if (!PassBySound || forwardDist < 0) { flewPast = true; } else { flewPast = false; }
                if (ColliderAlwaysActive && !EntityControl.IsOwner && BombRigid && !BombRigid.isKinematic && SAVControl)
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
            BombRigid.position = transform.position;
            SendCustomEventDelayedFrames(nameof(ensureNoSelfCollision), 1);
        }
        bool flewPast = true;
        void LateUpdate()
        {
            if (Exploding) return;
            if (!flewPast)
            {
                float forwardDist = Vector3.Dot(transform.forward, Networking.LocalPlayer.GetPosition() - transform.position);
                if (forwardDist < 0)
                {
                    Vector3 flypastPos = transform.position + transform.forward * forwardDist;
                    if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), flypastPos) < PassBySound_distance)
                    {
                        PassBySound.position = flypastPos;
                        PassBySound.SetParent(null);
                        PassBySound.gameObject.SetActive(true);
                    }
                    flewPast = true;
                }
            }
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
            UnderWater = false;
            hitwater = false;
            DirectHitObjectScript = null;
            ShockwaveActive = false;
            BombRigid.drag = DragStart;
            if (PassBySound)
            {
                PassBySound.SetParent(transform);
                PassBySound.gameObject.SetActive(false);
                PassBySound.localPosition = Vector3.zero;
            }
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
                SaccTarget HitTarget = other.gameObject.GetComponent<SaccTarget>();
                if (HitVehicle || HitTarget)
                {
                    float Armor = HitVehicle.ArmorStrength;
                    foreach (Transform child in other.collider.transform)
                    {
                        string pname = child.name;
                        if (pname.StartsWith("a:"))
                        {
                            if (float.TryParse(pname.Substring(2), out float ar))
                            {
                                if (ar > 0)
                                {
                                    Armor = ar;
                                }
                            }
                        }
                        // else if .. // could add a value for NoDamageBelow here
                    }

                    if (HitVehicle)
                    {
                        float dmg = BombDamage / Armor;
                        if (dmg > HitVehicle.NoDamageBelow || dmg < 0)
                        {
                            HitVehicle.WeaponDamageVehicle(dmg, EntityControl.gameObject, event_WeaponType);
                            DirectHitObjectScript = HitVehicle;
                        }
                    }
                    else if (HitTarget)
                    {
                        float dmg = BombDamage / Armor;
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
        public void Explode()
        {
            if (Exploding) return;
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
            flewPast = true;
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);


            if (SplashRadius == 0) return;
            if (ExpandingShockwave)
            {
                CurrentShockwave = 0;
                _ExpandingShockwave_Speed = ExpandingShockwave_Speed;
            }
            else
            {
                CurrentShockwave = SplashRadius;
                _ExpandingShockwave_Speed = 0;
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
                                        float SplashDamage = BombDamage * DamageFalloff;
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
                                            float SplashDamage = BombDamage * DamageFalloff;
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
                                    float SplashDamage = BombDamage * DamageFalloff;
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
                if (shockWaveSphere) SendCustomEventDelayedFrames(nameof(disableShockWaveSphere), 1);// so it's max size is visible, and it's visible for at least 1 frame
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