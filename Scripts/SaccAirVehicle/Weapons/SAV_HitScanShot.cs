
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_HitScanShot : UdonSharpBehaviour
    {
        public UdonSharpBehaviour BombLauncherControl;
        [Tooltip("Damage dealth to target:")]
        public float BeamDamage = 320;
        public GameObject BeamMesh;
        public float BeamLifeTime = 0.5f;
        public float TotalLifeTime = 1;
        public float BeamWidth = 1f;
        public float BeamRange = 3000f;
        [Tooltip("event_WeaponType is sent with damage and kill events, but not used for anything in the base prefab.\n0=None/Suicide,1=Gun,2=AAM,3=AGM,4=Bomb,5=Rocket,6=Cannon,7=Laser,8=Beam,9=Torpedo,10=VLS,11=Javelin,12=Railgun, anything else is undefined (custom) 0-255")]
        [SerializeField] private byte event_WeaponType = 8;
        [SerializeField] private bool ExplodeIfNoHit = false;
        [SerializeField] private bool RunFunctionOnHit = false;
        [SerializeField] private string Func_Name = "_interact";
        [SerializeField] private float HitForce = 0f;
        [Tooltip("Bad for vehicles, pilot will lose control if shot at.")]
        [SerializeField] private bool TakeOwnerOfHitObject = false;
        [Header("Knockback & Splash damage")]
        [SerializeField] float SplashRadius = 10;
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
        [Tooltip("should be a default unity sphere (radius 0.5)")]
        [SerializeField] private Transform shockWaveSphere;
        [Space]
        [Tooltip("Tick this to use this script for something not attached to a SaccEntity.")]
        [SerializeField] bool NoSaccEntity;
        [Tooltip("Useful if firing from a script that doesn't track owner, ai-controlled weapons?")]
        [SerializeField] bool OwnerAlwaysMaster;
        private UdonSharpBehaviour DirectHitObjectScript = null;
        public GameObject Explosion;
        public GameObject DamageParticles;
        public GameObject LaserParticle;
        private SaccEntity EntityControl;
        private bool IsOwner;
        bool initialized;
        private void Initialize()
        {
            initialized = true;
            if (!NoSaccEntity)
                EntityControl = (SaccEntity)BombLauncherControl.GetProgramVariable("EntityControl");
            HitRBs = new Rigidbody[Shockwave_max_targets];
            HitTargets = new SaccTarget[Shockwave_max_targets];
            // if (EntityControl) { VehicleCenterOfMass = EntityControl.CenterOfMass; }
        }
        Vector3 HitPosition;
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
            if (OwnerAlwaysMaster)
                IsOwner = Networking.Master.isLocal;
            else
                IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner");
            SendCustomEventDelayedSeconds(nameof(KillBeam), BeamLifeTime);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), TotalLifeTime);
            RaycastHit targetpoint;
            Vector3 beamscale = Vector3.one;
            if (Physics.Raycast(transform.position, transform.forward, out targetpoint, BeamRange, 141329 /* Default, Water, Environment, Pickup and Walkthrough */, QueryTriggerInteraction.Ignore))
            {
                beamscale.z = Vector3.Distance(transform.position, targetpoint.point);
                HitPosition = targetpoint.point;
                if (targetpoint.collider)
                {
                    Rigidbody TargetRigid = targetpoint.collider.attachedRigidbody;
                    if (IsOwner)
                    {
                        SaccEntity HitVehicle = null;
                        if (TargetRigid)
                            HitVehicle = TargetRigid.gameObject.GetComponent<SaccEntity>();
                        else
                            HitVehicle = targetpoint.collider.gameObject.GetComponent<SaccEntity>();
                        if (HitVehicle)
                        {
                            float Armor = 1; // initial value is never used
                            bool customArmorValueFound = false;
                            foreach (Transform child in targetpoint.collider.transform)
                            {
                                string pname = child.name;
                                if (pname.StartsWith("a:"))
                                {
                                    if (float.TryParse(pname.Substring(2), out float ar))
                                    {
                                        if (ar > 0)
                                        {
                                            Armor = ar;
                                            customArmorValueFound = true;
                                        }
                                    }
                                }
                                // else if .. // could add a value for NoDamageBelow here
                            }
                            if (!customArmorValueFound) Armor = HitVehicle.ArmorStrength;
                            float dmg = BeamDamage / Armor;
                            if (dmg > HitVehicle.NoDamageBelow || dmg < 0)
                            {
                                HitVehicle.WeaponDamageVehicle(dmg, EntityControl ? EntityControl.gameObject : null, event_WeaponType);
                                DirectHitObjectScript = HitVehicle;
                            }
                        }
                        if (HitForce != 0)
                        {
                            if (Networking.LocalPlayer.IsOwner(targetpoint.collider.gameObject))
                            {
                                TargetRigid.AddForceAtPosition(transform.forward * HitForce, HitPosition, ForceMode.Impulse);
                                if (HitVehicle) HitVehicle.SendEventToExtensions("SFEXT_L_WakeUp");
                            }
                            else if (TakeOwnerOfHitObject)
                            {
                                Networking.SetOwner(Networking.LocalPlayer, targetpoint.collider.gameObject);
                                TargetRigid.AddForceAtPosition(transform.forward * HitForce, HitPosition, ForceMode.Impulse);
                                if (HitVehicle) HitVehicle.SendEventToExtensions("SFEXT_L_WakeUp");
                            }
                        }
                        SaccTarget HitTarget = targetpoint.collider.gameObject.GetComponent<SaccTarget>();
                        if (!HitTarget && TargetRigid)
                        {
                            HitTarget = TargetRigid.gameObject.GetComponent<SaccTarget>();
                        }
                        if (HitTarget)
                        {
                            float Armor = 1; // initial value is never used
                            bool customArmorValueFound = false;
                            foreach (Transform child in targetpoint.collider.transform)
                            {
                                string pname = child.name;
                                if (pname.StartsWith("a:"))
                                {
                                    if (float.TryParse(pname.Substring(2), out float ar))
                                    {
                                        if (ar > 0)
                                        {
                                            Armor = ar;
                                            customArmorValueFound = true;
                                        }
                                    }
                                }
                                // else if .. // could add a value for NoDamageBelow here
                            }
                            if (!customArmorValueFound) Armor = HitTarget.ArmorStrength;
                            float dmg = BeamDamage / Armor;
                            if (dmg > HitTarget.NoDamageBelow || dmg < 0)
                            {
                                HitTarget.WeaponDamageTarget(dmg, EntityControl ? EntityControl.gameObject : null, event_WeaponType);
                                DirectHitObjectScript = HitTarget;
                            }
                        }
                        if (RunFunctionOnHit)
                        {
                            UdonSharpBehaviour TargetScript = targetpoint.collider.gameObject.GetComponent<UdonSharpBehaviour>();
                            if (TargetScript)
                            {
                                TargetScript.SendCustomEvent(Func_Name);
                            }
                            RaycastHit targetTrigger;
                            if (Physics.Raycast(transform.position, transform.forward, out targetTrigger, BeamRange, 141329 /* Default, Water, Environment, Pickup and Walkthrough */, QueryTriggerInteraction.Collide))
                            {
                                if (targetTrigger.collider)
                                {
                                    UdonSharpBehaviour targetTriggerScript;
                                    if (targetTrigger.collider.attachedRigidbody)
                                        targetTriggerScript = targetTrigger.collider.attachedRigidbody.gameObject.GetComponent<UdonSharpBehaviour>();
                                    else
                                        targetTriggerScript = targetTrigger.collider.gameObject.GetComponent<UdonSharpBehaviour>();
                                    if (TargetScript != targetTriggerScript)
                                    {
                                        if (targetTriggerScript)
                                        {
                                            targetTriggerScript.SendCustomEvent(Func_Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Explode();
            }
            else
            {
                HitPosition = transform.position + transform.forward * BeamRange;
                beamscale.z = BeamRange;
                if (ExplodeIfNoHit)
                {
                    Explode();
                }
            }
            if (BeamMesh)
            {
                BeamMesh.SetActive(true);
                beamscale.x *= BeamWidth;
                beamscale.y *= BeamWidth;
                BeamMesh.transform.localScale = beamscale;
            }
            if (LaserParticle)
            {
                LaserParticle.transform.rotation = transform.rotation;
                LaserParticle.transform.position = (transform.position + HitPosition) * .5f;


                ParticleSystem LaserParticle_P = LaserParticle.GetComponent<ParticleSystem>();
                ParticleSystem.ShapeModule LaserParticle_S = LaserParticle_P.shape;
                LaserParticle_S.radius = (LaserParticle.transform.position - HitPosition).magnitude;
                LaserParticle_P.Emit((int)LaserParticle_S.radius);
            }
        }
        [System.NonSerializedAttribute] public bool Exploding = false;
        void Explode()
        {
            if (Exploding) return;
            if (Explosion)
            {
                Explosion.transform.position = HitPosition;
                Explosion.SetActive(true);
            }
            if (IsOwner)
            {
                if (DamageParticles)
                {
                    DamageParticles.transform.position = HitPosition;
                    DamageParticles.SetActive(true);
                }
            }
            if (SplashRadius == 0) return;
            if (ExpandingShockwave)
            {
                CurrentShockwave = 0;
                _ExpandingShockwave_Speed = ExpandingShockwave_Speed;
            }
            else
            {
                _ExpandingShockwave_Speed = 0;
                CurrentShockwave = SplashRadius;
            }
            if (!ShockwaveActive)
            {
                if (shockWaveSphere)
                {
                    shockWaveSphere.transform.position = HitPosition;
                    shockWaveSphere.gameObject.SetActive(true);
                }
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
            int numHits = Physics.OverlapSphereNonAlloc(HitPosition, CurrentShockwave, hitobjs);
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

                        Vector3 explosionDirRB = thisRB.worldCenterOfMass - HitPosition;
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
                                        float SplashDamage = BeamDamage * DamageFalloff;
                                        if (SplashDamage > hitEntity.NoDamageBelow || SplashDamage < 0)
                                        {
                                            hitEntity.WeaponDamageVehicle(SplashDamage, EntityControl ? EntityControl.gameObject : null, event_WeaponType);
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
                                            float SplashDamage = BeamDamage * DamageFalloff;
                                            if (SplashDamage > hitTarget.NoDamageBelow || SplashDamage < 0)
                                            {
                                                hitTarget.WeaponDamageTarget(SplashDamage, EntityControl ? EntityControl.gameObject : null, event_WeaponType);
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
                                    float SplashDamage = BeamDamage * DamageFalloff;
                                    if (SplashDamage > thisTarget.NoDamageBelow || SplashDamage < 0)
                                    {
                                        thisTarget.WeaponDamageTarget(SplashDamage, EntityControl ? EntityControl.gameObject : null, event_WeaponType);
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
                if (Vector3.Distance(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, HitPosition) < CurrentShockwave)
                {
                    Vector3 explosionDir = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - HitPosition;
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
        public void KillBeam()
        {
            if (BeamMesh) { BeamMesh.SetActive(false); }
            if (DamageParticles) { DamageParticles.SetActive(false); }
        }
        public void MoveBackToPool()
        {
            Exploding = false;
            if (Explosion) { Explosion.SetActive(false); }
            if (DamageParticles) { DamageParticles.SetActive(false); }
            gameObject.SetActive(false);
            transform.SetParent(BombLauncherControl.transform);
            transform.localPosition = Vector3.zero;
            DirectHitObjectScript = null;
            ShockwaveActive = false;
        }
    }
}