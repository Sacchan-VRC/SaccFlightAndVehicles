
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_RocketController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour LauncherControl;
        [Tooltip("Bomb will explode after this time")]
        public float MaxLifetime = 15;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        public float ExplosionLifeTime = 10;
        [Tooltip("Enable collider this long after missile has launched (collider is disabled to prevent hitting your own vehicle")]
        public float ColliderEnableDelay = .08f;
        [Tooltip("Play a random one of these explosion sounds")]
        public AudioSource[] ExplosionSounds;
        [Tooltip("Play a random one of these explosion sounds when hitting water")]
        public AudioSource[] WaterExplosionSounds;
        [Tooltip("Spawn bomb at a random angle up to this number of degrees")]
        public float AngleRandomization = 0;
        private Animator RocketAnimator;
        private Rigidbody RocketRigid;
        private SaccEntity EntityControl;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private CapsuleCollider RocketCollider;
        private Transform VehicleCenterOfMass;
        private bool hitwater;
        private bool IsOwner;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private void Initialize()
        {
            initialized = true;
            EntityControl = (SaccEntity)LauncherControl.GetProgramVariable("EntityControl");
            VehicleCenterOfMass = EntityControl.CenterOfMass;
            RocketCollider = GetComponent<CapsuleCollider>();
            RocketRigid = GetComponent<Rigidbody>();
            RocketAnimator = GetComponent<Animator>();
        }
        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
            if (EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)LauncherControl.GetProgramVariable("IsOwner"); }
            SendCustomEventDelayedSeconds(nameof(EnableCollider), ColliderEnableDelay);
            SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
            LifeTimeExplodesSent++;
        }
        public void EnableCollider()
        { RocketCollider.enabled = true; }
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
            RocketAnimator.WriteDefaultValues();
            gameObject.SetActive(false);
            transform.SetParent(LauncherControl.transform);
            RocketCollider.enabled = false;
            RocketRigid.constraints = RigidbodyConstraints.None;
            RocketRigid.angularVelocity = Vector3.zero;
            transform.localPosition = Vector3.zero;
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
            if (RocketRigid)
            {
                RocketRigid.constraints = RigidbodyConstraints.FreezePosition;
                RocketRigid.velocity = Vector3.zero;
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
            RocketCollider.enabled = false;
            if (IsOwner)
            { RocketAnimator.SetTrigger("explodeowner"); }
            else { RocketAnimator.SetTrigger("explode"); }
            RocketAnimator.SetBool("hitwater", hitwater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);
        }
    }
}