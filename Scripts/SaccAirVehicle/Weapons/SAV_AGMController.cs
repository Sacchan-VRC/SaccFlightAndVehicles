
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
        private Animator MissileAnimator;
        private SaccEntity EntityControl;
        private bool StartTrack = false;
        private Transform VehicleCenterOfMass;
        private Transform TargetTransform;
        private Vector3 TargetOffset;
        private bool ColliderActive = false;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private bool IsOwner = false;
        private CapsuleCollider AGMCollider;
        private Rigidbody AGMRigid;
        private ConstantForce MissileConstant;
        private bool hitwater;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private void Initialize()
        {
            initialized = true;
            EntityControl = (SaccEntity)AGMLauncherControl.GetProgramVariable("EntityControl");
            VehicleCenterOfMass = EntityControl.CenterOfMass;
            AGMCollider = gameObject.GetComponent<CapsuleCollider>();
            AGMRigid = gameObject.GetComponent<Rigidbody>();
            MissileConstant = GetComponent<ConstantForce>();
            MissileAnimator = gameObject.GetComponent<Animator>();
        }
        public void ThrowMissile()
        {
            AGMRigid.velocity += (ThrowSpaceVehicle ? EntityControl.transform.TransformDirection(ThrowVelocity) : transform.TransformDirection(ThrowVelocity));
        }
        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            TargetTransform = (Transform)AGMLauncherControl.GetProgramVariable("TrackedTransform");
            TargetOffset = (Vector3)AGMLauncherControl.GetProgramVariable("TrackedObjectOffset");
            if (EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)AGMLauncherControl.GetProgramVariable("IsOwner"); }
            SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
            LifeTimeExplodesSent++;
            SendCustomEventDelayedSeconds(nameof(StartTracking), FlyStraightTime);
            SendCustomEventDelayedFrames(nameof(ThrowMissile), 1);//doesn't work if done this frame

            //LateUpdate runs one time after MoveBackToPool so these must be here
            ColliderActive = false;
            MissileConstant.relativeTorque = Vector3.zero;
            MissileConstant.relativeForce = Vector3.zero;
        }
        void LateUpdate()
        {
            float sidespeed = Vector3.Dot(AGMRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(AGMRigid.velocity, transform.up);
            float ConstantRelativeForce = MissileConstant.relativeForce.z;
            Vector3 NewConstantRelativeForce = new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, ConstantRelativeForce);
            MissileConstant.relativeForce = NewConstantRelativeForce;
            Vector3 missileToTargetVector = TargetTransform.TransformPoint(TargetOffset) - transform.position;
            float DeltaTime = Time.deltaTime;
            if (!ColliderActive)
            {
                if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
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
            }
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
            transform.localPosition = Vector3.zero;
            StartTrack = false;
            Exploding = false;
        }
        private void OnCollisionEnter(Collision other)
        { if (!Exploding) { hitwater = false; Explode(); } }
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
            { AGMRigid.constraints = RigidbodyConstraints.FreezePosition; }
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
        }
    }
}