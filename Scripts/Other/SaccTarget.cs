
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccTarget : UdonSharpBehaviour
    {
        [UdonSynced] public float HitPoints = 30f;
        [Tooltip("Particle collisions will do this much damage")]
        public float DamageFromBullet = 10f;
        [Tooltip("Direct hits from missiles or any rigidbody will do this much damage")]
        public float DamageFromCollision = 30f;
        [Tooltip("How long it takes to respawn")]
        public float RespawnDelay = 10f;
        [Tooltip("Other UdonBehaviours that will recieve the event 'Explode'")]
        public UdonSharpBehaviour[] ExplodeOther;
        [Tooltip("Ignore particles hitting the object?")]
        public bool DisableBulletHitEvent = false;
        private Animator TargetAnimator;
        private float FullHealth;
        private VRCPlayerApi localPlayer;
        [Tooltip("Using the particle system name damage system, ignore damage events below this level of damage -10 to 14")]
        public int MinDamageLevel = -10;
        [System.NonSerializedAttribute] public GameObject LastHitParticle;
        [System.NonSerializedAttribute] public SaccEntity LastAttacker;
        bool dead;
        float lastUpdateTime;
        void Start()
        {
            TargetAnimator = gameObject.GetComponent<Animator>();
            FullHealth = HitPoints;
            localPlayer = Networking.LocalPlayer;
            TargetAnimator.SetBool("dead", false);
            TargetAnimator.SetFloat("healthpc", HitPoints / FullHealth);
        }
        void OnParticleCollision(GameObject other)//hit by bullet
        {
            if (!other || dead || DisableBulletHitEvent) { return; }
            LastHitParticle = other;

            int index = -1;
            string pname = string.Empty;
            if (other.transform.childCount > 0)
            {
                pname = other.transform.GetChild(0).name;
                index = pname.LastIndexOf(':');
            }
            int dmg = 1;
            bool More = true;
            if (index > -1)
            {
                pname = pname.Substring(index);
                if (pname.Length == 3)
                {
                    if (pname[1] == 'x')
                    {
                        if (pname[2] >= '0' && pname[2] <= '9')
                        {
                            //damage reduction using case:
                            dmg = pname[2] - 48;
                            LastHitBulletDamageMulti = 1 / (float)(dmg);
                            More = false;
                        }
                    }
                    else if (pname[1] >= '0' && pname[1] <= '9')
                    {
                        if (pname[2] >= '0' && pname[2] <= '9')
                        {
                            //damage increase using case:
                            dmg = 10 * (pname[1] - 48);
                            dmg += pname[2] - 48;
                            LastHitBulletDamageMulti = dmg == 1 ? 1 : Mathf.Pow(2, dmg - 1);
                            More = true;
                        }
                    }
                }
            }
            if (More)
            { if (dmg < MinDamageLevel) return; }
            else
            { if (-dmg < MinDamageLevel) return; }

            if (Time.time - lastUpdateTime > 1) { Networking.SetOwner(localPlayer, gameObject); }

            //Try to find the saccentity that shot at us
            GameObject EnemyObjs = other;
            SaccEntity EnemyEntityControl = null;
            //search up the hierarchy to find the saccentity directly
            while (!EnemyEntityControl && EnemyObjs.transform.parent)
            {
                EnemyObjs = EnemyObjs.transform.parent.gameObject;
                EnemyEntityControl = EnemyObjs.GetComponent<SaccEntity>();
            }
            LastAttacker = EnemyEntityControl;
            //if failed to find it, search up the hierarchy for an udonsharpbehaviour with a reference to the saccentity (for instantiated missiles etc)
            if (!EnemyEntityControl)
            {
                EnemyObjs = other;
                UdonBehaviour EnemyUdonBehaviour = null;
                while (!EnemyUdonBehaviour && EnemyObjs.transform.parent)
                {
                    EnemyObjs = EnemyObjs.transform.parent.gameObject;
                    EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
                }
                if (EnemyUdonBehaviour)
                { LastAttacker = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
            }
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
                SendNetworkUpdate();
            }
            else { SendDamageEvent(dmg, More); }
            if (LastAttacker && LastAttacker != this) { LastAttacker.SendEventToExtensions("SFEXT_L_DamageFeedback"); }
        }
        public void SendDamageEvent(int dmg, bool More)
        {
            if (More)//More than default damage
            {
                switch (dmg)
                {
                    case 2:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage2x));
                        break;
                    case 3:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage4x));
                        break;
                    case 4:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage8x));
                        break;
                    case 5:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage16x));
                        break;
                    case 6:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage32x));
                        break;
                    case 7:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage64x));
                        break;
                    case 8:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage128x));
                        break;
                    case 9:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage256x));
                        break;
                    case 10:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage512x));
                        break;
                    case 11:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage1024x));
                        break;
                    case 12:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage2048x));
                        break;
                    case 13:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage4096x));
                        break;
                    case 14:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage8192x));
                        break;
                    default:
                        if (dmg != 1) { Debug.LogWarning("Invalid bullet damage, using default"); }
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageDefault));
                        break;
                }
            }
            else//less that default damage
            {
                switch (dmg)
                {
                    case 2:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageHalf));
                        break;
                    case 3:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageThird));
                        break;
                    case 4:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageQuarter));
                        break;
                    case 5:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageFifth));
                        break;
                    case 6:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSixth));
                        break;
                    case 7:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSeventh));
                        break;
                    case 8:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageEighth));
                        break;
                    case 9:
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageNinth));
                        break;
                    default:
                        if (dmg != 1) { Debug.LogWarning("Invalid bullet damage, using default"); }
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageDefault));
                        break;
                }
            }
        }
        [System.NonSerializedAttribute] public float LastHitBulletDamageMulti = 1;
        public void BulletDamageNinth()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .11111111111111f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageEighth()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .125f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageSeventh()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .14285714285714f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageSixth()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .16666666666666f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageFifth()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .2f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageQuarter()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .25f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageThird()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .33333333333333f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageHalf()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = .5f;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamageDefault()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 1;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage2x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 2;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage4x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 4;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage8x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 8;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage16x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 16;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage32x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 32;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage64x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 64;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage128x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 128;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage256x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 256;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage512x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 512;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage1024x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 1024;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage2048x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 2048;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage4096x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 4096;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        public void BulletDamage8192x()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            LastHitBulletDamageMulti = 8192;
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        private void OnCollisionEnter(Collision other)
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            HitPoints -= DamageFromCollision;
            SendNetworkUpdate();
        }
        public void RespawnTarget()
        {
            respawning = false;
            HitPoints = FullHealth;
            SendNetworkUpdate();
        }
        bool respawning;
        public void SendNetworkUpdate()
        {
            if (HitPoints <= 0)
            {
                HitPoints = 0;
                if (!respawning && RespawnDelay > 0)
                {
                    respawning = true;
                    SendCustomEventDelayedSeconds(nameof(RespawnTarget), RespawnDelay);
                }
            }
            RequestSerialization();
            OnDeserialization();
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (dead)
            {
                SendNetworkUpdate();
            }
        }
        public void Explode()
        {
            if (!localPlayer.IsOwner(gameObject)) return;
            HitPoints = 0;
            SendNetworkUpdate();
        }
        bool deadlast;
        public override void OnDeserialization()
        {
            lastUpdateTime = Time.time;
            dead = HitPoints <= 0f;
            TargetAnimator.SetBool("dead", dead);
            TargetAnimator.SetFloat("healthpc", HitPoints / FullHealth);
            if (dead && !deadlast)
            {
                foreach (UdonSharpBehaviour Exploder in ExplodeOther)
                {
                    if (Exploder)
                    {
                        Exploder.SendCustomEvent(nameof(Explode));
                    }
                }
            }
            deadlast = dead;
        }
    }
}