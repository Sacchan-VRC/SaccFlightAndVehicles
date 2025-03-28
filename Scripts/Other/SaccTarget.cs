
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccTarget : UdonSharpBehaviour
    {
        [Header("Do not put colliders on child objects, it wont work properly (unless rigidbody?)")]
        [UdonSynced] public float HitPoints = 30f;
        [Tooltip("Particle collisions will do this much damage")]
        public float DamageFromBullet = 10f;
        [Tooltip("Direct hits from missiles or any rigidbody will do this much damage")]
        public float DamageFromCollision = 30f;
        [Tooltip("How long it takes to respawn, 0 to never respawn")]
        public float RespawnDelay = 10f;
        [Tooltip("Other UdonBehaviours that will recieve the event 'Explode'")]
        public UdonSharpBehaviour[] ExplodeOther;
        [Tooltip("Ignore particles hitting the object?")]
        public bool DisableBulletHitEvent = false;
        [Tooltip("Using the particle damage system, ignore damage events below this level of damage")]
        [Range(-9, 14)]
        public int BulletArmorLevel = -9;
        private Animator TargetAnimator;
        private float FullHealth;
        private VRCPlayerApi localPlayer;
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
        void OnParticleCollision(GameObject other)
        {
            if (!other || dead || DisableBulletHitEvent) { return; }//avatars can't hurt you, and you can't get hurt when you're dead
            LastHitParticle = other;

            int dmg = 1;
            if (other.transform.childCount > 0)
            {
                string pname = other.transform.GetChild(0).name;
                getDamageValue(pname, ref dmg);
            }
            if (dmg < BulletArmorLevel) return;

            if (!Networking.LocalPlayer.IsOwner(gameObject) && Time.time - lastUpdateTime > 1)
                Networking.SetOwner(localPlayer, gameObject);

            if (Networking.LocalPlayer.IsOwner(gameObject)) BulletDamage_Owner();
            else WeaponDamageTarget(dmg, other);
        }
        public void WeaponDamageTarget(int dmg, GameObject damagingObject)
        {
            //Try to find the saccentity that shot at us
            GameObject EnemyObjs = damagingObject;
            SaccEntity EnemyEntityControl = damagingObject.GetComponent<SaccEntity>();
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
                EnemyObjs = damagingObject;
                UdonBehaviour EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
                while (!EnemyUdonBehaviour && EnemyObjs.transform.parent)
                {
                    EnemyObjs = EnemyObjs.transform.parent.gameObject;
                    EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
                }
                if (EnemyUdonBehaviour)
                { LastAttacker = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
            }
            SendDamageEvent(dmg);
            if (LastAttacker && LastAttacker != this) { LastAttacker.SendEventToExtensions("SFEXT_L_DamageFeedback"); }
        }
        void getDamageValue(string name, ref int dmg)
        {
            int index = name.LastIndexOf(':');
            if (index > -1)
            {
                name = name.Substring(index);
                if (name.Length == 3)
                {
                    if (name[1] == 'x')
                    {
                        if (name[2] >= '0' && name[2] <= '9')
                        {
                            dmg = name[2] - 48;
                            LastHitBulletDamageMulti = 1 / (float)(dmg);
                            dmg = -dmg;
                        }
                    }
                    else if (name[1] >= '0' && name[1] <= '9')
                    {
                        if (name[2] >= '0' && name[2] <= '9')
                        {
                            dmg = 10 * (name[1] - 48);
                            dmg += name[2] - 48;
                            LastHitBulletDamageMulti = dmg == 1 ? 1 : Mathf.Pow(2, dmg - 1);
                        }
                    }
                }
            }
        }
        public void SendDamageEvent(int dmg)
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

                case -2:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageHalf));
                    break;
                case -3:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageThird));
                    break;
                case -4:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageQuarter));
                    break;
                case -5:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageFifth));
                    break;
                case -6:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSixth));
                    break;
                case -7:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSeventh));
                    break;
                case -8:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageEighth));
                    break;
                case -9:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageNinth));
                    break;
                default:
                    if (dmg != 1) { Debug.LogWarning("Invalid bullet damage, using default"); }
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageDefault));
                    break;
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
        public void BulletDamage_Owner()
        {
            HitPoints -= DamageFromBullet * LastHitBulletDamageMulti;
            SendNetworkUpdate();
        }
        private void OnCollisionEnter(Collision other)
        {
            if (!other.collider) return;
            // the owner of unsynced objects(missiles etc) returns the master.
            // so we need to find owner another way
            SAV_AAMController aam = other.collider.GetComponent<SAV_AAMController>();
            bool isColliderOwner;
            if (aam)
                isColliderOwner = (bool)aam.AAMLauncherControl.GetProgramVariable("IsOwner");
            else
            {
                SAV_AGMController agm = other.collider.GetComponent<SAV_AGMController>();
                if (agm)
                    isColliderOwner = (bool)agm.AGMLauncherControl.GetProgramVariable("IsOwner");
                else
                {
                    SAV_BombController bomb = other.collider.GetComponent<SAV_BombController>();
                    if (bomb)
                        isColliderOwner = (bool)bomb.BombLauncherControl.GetProgramVariable("IsOwner");
                    else
                        isColliderOwner = localPlayer.IsOwner(other.collider.gameObject);
                }
            }
            if (isColliderOwner && !localPlayer.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }
            HitPoints -= DamageFromCollision;
            SendNetworkUpdate();
        }
        public void RespawnTarget()
        {
            if (!localPlayer.IsOwner(gameObject))
            { Networking.SetOwner(localPlayer, gameObject); }
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
            if (!player.isLocal) return;
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
            TargetAnimator.SetTrigger("hit");
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