
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccTarget : UdonSharpBehaviour
    {
        public float HitPoints = 30f;
        [Tooltip("Particle collisions will do tih smuch damage")]
        public float DamageFromBullet = 10f;
        [Tooltip("Direct hits from missiles or any rigidbody will do this much damage")]
        public float DamageFromCollision = 30f;
        [Tooltip("Other UdonBehaviours that will recieve the event 'Explode'")]
        public UdonSharpBehaviour[] ExplodeOther;
        private Animator TargetAnimator;
        private float FullHealth;
        private VRCPlayerApi localPlayer;
        void Start()
        {
            TargetAnimator = gameObject.GetComponent<Animator>();
            FullHealth = HitPoints;
            localPlayer = Networking.LocalPlayer;
        }
        void OnParticleCollision(GameObject other)//hit by bullet
        {
            if (!other) return;
            if (HitPoints <= DamageFromBullet)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode)); }
            else
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TargetTakeDamage)); }
        }
        private void OnCollisionEnter(Collision other)
        {
            if (other == null) return;
            if (HitPoints <= DamageFromCollision)
            { Explode(); }
            else
            { TargetTakeDamageCollision(); }
        }
        public void TargetTakeDamage()
        { HitPoints -= DamageFromBullet; }
        public void TargetTakeDamageCollision()
        { HitPoints -= DamageFromCollision; }
        public void Explode()
        {
            TargetAnimator.SetTrigger("explode");
            HitPoints = FullHealth;
            foreach (UdonSharpBehaviour Exploder in ExplodeOther)
            {
                if (Exploder)
                {
                    Exploder.SendCustomEvent(nameof(Explode));
                }
            }
        }
    }
}