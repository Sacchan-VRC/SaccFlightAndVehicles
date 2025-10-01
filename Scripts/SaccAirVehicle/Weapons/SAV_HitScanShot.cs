
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
        [Tooltip("Maximum number of rigidboes that can be blasted, to save performance")]
        [SerializeField] private int Shockwave_max_targets = 30;
        [Tooltip("should be a default unity sphere (radius 0.5)")]
        [SerializeField] private Transform shockWaveSphere;
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
            EntityControl = (SaccEntity)BombLauncherControl.GetProgramVariable("EntityControl");
            HitRBs = new Rigidbody[Shockwave_max_targets];
            HitTargets = new SaccTarget[Shockwave_max_targets];
            // if (EntityControl) { VehicleCenterOfMass = EntityControl.CenterOfMass; }
        }
        Vector3 HitPosition;
        public void EnableWeapon()
        {
            if (!initialized) { Initialize(); }
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
                            float Armor = -1;
                            bool ColliderHasArmorValue = false;
                            if (targetpoint.collider.transform.childCount > 0)
                            {
                                string pname = targetpoint.collider.transform.GetChild(0).name;
                                ColliderHasArmorValue = getArmorValue(pname, ref Armor);
                            }
                            if (!ColliderHasArmorValue)
                            {
                                Armor = HitVehicle.ArmorStrength;
                            }
                            float dmg = BeamDamage / Armor;
                            if (dmg > HitVehicle.NoDamageBelow)
                            {
                                HitVehicle.WeaponDamageVehicle(dmg, EntityControl.gameObject, event_WeaponType);
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
                            float Armor = -1;
                            bool ColliderHasArmorValue = false;
                            if (targetpoint.collider.transform.childCount > 0)
                            {
                                string pname = targetpoint.collider.transform.GetChild(0).name;
                                ColliderHasArmorValue = getArmorValue(pname, ref Armor);
                            }
                            if (!ColliderHasArmorValue)
                            {
                                Armor = HitTarget.ArmorStrength;
                            }
                            float dmg = BeamDamage / Armor;
                            if (dmg > HitTarget.NoDamageBelow)
                            {
                                HitTarget.WeaponDamageTarget(dmg, EntityControl.gameObject, event_WeaponType);
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
                                        if (SplashDamage > hitEntity.NoDamageBelow)
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
                                            float SplashDamage = BeamDamage * DamageFalloff;
                                            if (SplashDamage > hitTarget.NoDamageBelow)
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
                                    float SplashDamage = BeamDamage * DamageFalloff;
                                    if (SplashDamage > thisTarget.NoDamageBelow)
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