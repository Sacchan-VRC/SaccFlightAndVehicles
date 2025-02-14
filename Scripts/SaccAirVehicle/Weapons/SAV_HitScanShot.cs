
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
        [Tooltip("Using the particle damage system, damage level:")]
        [Range(-9, 14)]
        public int DamageLevel = 0;
        public GameObject BeamMesh;
        public float BeamLifeTime = 0.5f;
        public float TotalLifeTime = 1;
        public float BeamWidth = 1f;
        public float BeamRange = 3000f;
        [SerializeField] private bool ExplodeIfNoHit = false;
        [SerializeField] private bool RunFunctionOnHit = false;
        [SerializeField] private string Func_Name = "_interact";
        [SerializeField] private float HitForce = 0f;
        [Tooltip("Bad for vehicles, pilot will lose control if shot at.")]
        [SerializeField] private bool TakeOwnerOfHitObject = false;
        public GameObject Explosion;
        public GameObject DamageParticles;
        public GameObject LaserParticle;
        private SaccEntity EntityControl;
        private bool IsOwner;
        public void EnableWeapon()
        {
            EntityControl = (SaccEntity)BombLauncherControl.GetProgramVariable("EntityControl");
            // if (EntityControl) { VehicleCenterOfMass = EntityControl.CenterOfMass; }
            if (EntityControl && EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner"); }
            SendCustomEventDelayedSeconds(nameof(KillBeam), BeamLifeTime);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), TotalLifeTime);
            RaycastHit targetpoint;
            Vector3 beamscale = Vector3.one;
            Vector3 HitPoint;
            if (Physics.Raycast(transform.position, transform.forward, out targetpoint, BeamRange, 141329 /* Default, Water, Environment, Pickup and Walkthrough */, QueryTriggerInteraction.Ignore))
            {
                beamscale.z = Vector3.Distance(transform.position, targetpoint.point);
                HitPoint = targetpoint.point;
                if (targetpoint.collider)
                {
                    Rigidbody TargetRigid = targetpoint.collider.attachedRigidbody;
                    if (IsOwner)
                    {
                        if (TargetRigid)
                        {
                            SaccEntity HitVehicle = TargetRigid.gameObject.GetComponent<SaccEntity>();
                            if (HitVehicle)
                            {
                                if (DamageLevel > HitVehicle.BulletArmorLevel)
                                    HitVehicle.WeaponDamageVehicle(DamageLevel, gameObject);
                            }
                            if (HitForce != 0)
                            {
                                if (Networking.LocalPlayer.IsOwner(targetpoint.collider.gameObject))
                                {
                                    TargetRigid.AddForceAtPosition(transform.forward * HitForce, HitPoint, ForceMode.Impulse);
                                    if (HitVehicle) HitVehicle.SendEventToExtensions("SFEXT_L_WakeUp");
                                }
                                else if (TakeOwnerOfHitObject)
                                {
                                    Networking.SetOwner(Networking.LocalPlayer, targetpoint.collider.gameObject);
                                    TargetRigid.AddForceAtPosition(transform.forward * HitForce, HitPoint, ForceMode.Impulse);
                                    if (HitVehicle) HitVehicle.SendEventToExtensions("SFEXT_L_WakeUp");
                                }
                            }
                        }
                        SaccTarget HitTarget = targetpoint.collider.gameObject.GetComponent<SaccTarget>();
                        if (HitTarget)
                        {
                            if (DamageLevel > HitTarget.BulletArmorLevel)
                                HitTarget.WeaponDamageTarget(DamageLevel, gameObject);
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
                Explode(HitPoint);
            }
            else
            {
                HitPoint = transform.position + transform.forward * BeamRange;
                beamscale.z = BeamRange;
            }
            if (ExplodeIfNoHit)
            {
                Explode(HitPoint);
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
                LaserParticle.transform.position = (transform.position + HitPoint) * .5f;


                ParticleSystem LaserParticle_P = LaserParticle.GetComponent<ParticleSystem>();
                ParticleSystem.ShapeModule LaserParticle_S = LaserParticle_P.shape;
                LaserParticle_S.radius = (LaserParticle.transform.position - HitPoint).magnitude;
                LaserParticle_P.Emit((int)LaserParticle_S.radius);
            }
        }
        void Explode(Vector3 hitpoint)
        {
            if (Explosion)
            {
                Explosion.transform.position = hitpoint;
                Explosion.SetActive(true);
            }
            if (IsOwner)
            {
                if (DamageParticles)
                {
                    DamageParticles.transform.position = hitpoint;
                    DamageParticles.SetActive(true);
                }
            }
        }
        public void KillBeam()
        {
            if (BeamMesh) { BeamMesh.SetActive(false); }
            if (DamageParticles) { DamageParticles.SetActive(false); }
        }
        public void MoveBackToPool()
        {
            if (Explosion) { Explosion.SetActive(false); }
            if (DamageParticles) { DamageParticles.SetActive(false); }
            gameObject.SetActive(false);
            transform.SetParent(BombLauncherControl.transform);
            transform.localPosition = Vector3.zero;
        }
    }
}