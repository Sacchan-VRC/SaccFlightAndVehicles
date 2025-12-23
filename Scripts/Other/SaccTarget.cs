
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccTarget : UdonSharpBehaviour
    {
        [Header("Do not put colliders on child objects, it wont work properly (unless rigidbody?)")]
        [UdonSynced] public float Health = 30f;
        [Tooltip("How long it takes to respawn, 0 to never respawn")]
        public float RespawnDelay = 10f;
        [Tooltip("Other UdonBehaviours that will recieve the event 'Explode'")]
        public UdonSharpBehaviour[] ExplodeOther;
        [Tooltip("Ignore particles hitting the object?")]
        public bool DisableBulletHitEvent = false;
        [Tooltip("All attack damage will be divided by this value")]
        public float ArmorStrength = 1f;
        [Tooltip("If an attack does less than this amount of damage, all damage will be discarded.")]
        public float NoDamageBelow = 0f;
        [Tooltip("Instantly explode locally instead of waiting for network confirmation if your client predicts target should, possible desync if target is healing when shot")]
        public bool PredictExplosion = true;
        [Space]
        [Tooltip("Send event when someone gets a kill on this target")]
        public bool SendKillEvents;
        public UdonBehaviour KillFeed;
        [SerializeField] private string[] TargetKilledMessages = { "%KILLER% destroyed a Target", };
        private Animator TargetAnimator;
        [System.NonSerialized] public float FullHealth;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public GameObject LastHitParticle;
        [System.NonSerializedAttribute] public SaccEntity LastAttacker;
        bool dead;
        float lastUpdateTime;
        void Start()
        {
            TargetAnimator = gameObject.GetComponent<Animator>();
            FullHealth = Health;
            localPlayer = Networking.LocalPlayer;
            TargetAnimator.SetBool("dead", false);
            TargetAnimator.SetFloat("healthpc", Health / FullHealth);
            if (ArmorStrength == 0) ArmorStrength = 0.000001f;
        }
        void OnParticleCollision(GameObject other)
        {
            if (!other || dead || DisableBulletHitEvent) { return; }//avatars can't hurt you, and you can't get hurt when you're dead
            LastHitParticle = other;
            byte weaponType = 1; // default weapon type
            float damage = 10f / ArmorStrength; // default damage

            // Loop through all children to find damage and weapon type
            foreach (Transform child in other.transform)
            {
                string pname = child.name;
                if (pname.StartsWith("d:"))
                {
                    if (float.TryParse(pname.Substring(2), out float dmg))
                    {
                        damage = dmg;
                    }
                }
                else if (pname.StartsWith("t:"))
                {
                    if (byte.TryParse(pname.Substring(2), out byte wt))
                    {
                        weaponType = wt;
                    }
                }
            }

            if (damage > 0 && damage < NoDamageBelow) return;
            WeaponDamageTarget(damage, other, weaponType);
        }
        public void WeaponDamageTarget(float damage, GameObject damagingObject, byte weaponType)
        {
            if (dead) return;
            SaccEntity EnemyEntityControl = null;
            if (damagingObject)
            {
                //Try to find the saccentity that shot at us
                GameObject EnemyObjs = damagingObject;
                EnemyEntityControl = damagingObject.GetComponent<SaccEntity>();
                //search up the hierarchy to find the saccentity directly
                while (!EnemyEntityControl && EnemyObjs.transform.parent)
                {
                    EnemyObjs = EnemyObjs.transform.parent.gameObject;
                    EnemyEntityControl = EnemyObjs.GetComponent<SaccEntity>();
                }
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
                    { EnemyEntityControl = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
                }
            }
            LastAttacker = EnemyEntityControl;
            LastHitDamage = damage;
            DamagePrediction();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Self, nameof(SendDamageEvent), damage, weaponType);//local
            QueueDamage(damage, weaponType);//send to others
            if (LastAttacker && LastAttacker != this) { LastAttacker.SendEventToExtensions("SFEXT_L_DamageFeedback"); }
        }
        float PredictedLastHitTime = -100, PredictedHealth;
        void DamagePrediction()
        {
            if (localPlayer.IsOwner(gameObject)) return;
            if (PredictExplosion)
            {
                if (Time.time - PredictedLastHitTime > 2)
                {
                    PredictedLastHitTime = Time.time;
                    PredictedHealth = Mathf.Min(Health - LastHitDamage, FullHealth);
                    if (!dead && PredictedHealth <= 0)
                    {
                        PredictExplode();
                    }
                }
                else
                {
                    PredictedLastHitTime = Time.time;
                    PredictedHealth = Mathf.Min(PredictedHealth - LastHitDamage, FullHealth);
                    if (!dead && PredictedHealth <= 0)
                    {
                        PredictExplode();
                    }
                }
            }
        }
        float LastDamageSentTime;
        const float DAMAGESENDINTERVAL = 0.2f;
        float QueuedDamage;
        byte QueuedWeaponType;
        void QueueDamage(float dmg, byte weaponType)
        {
            QueuedWeaponType = weaponType;// I don't think there's much point in making a more complicated system to do this properly for now.
            QueuedDamage += dmg;
            if (Time.time - LastDamageSentTime > DAMAGESENDINTERVAL)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Others, nameof(SendDamageEvent), QueuedDamage, weaponType);
                LastDamageSentTime = Time.time;
                QueuedDamage = 0;
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(sendQueuedDamage), DAMAGESENDINTERVAL);
            }
        }
        public void sendQueuedDamage()
        {
            if (Time.time - LastDamageSentTime > DAMAGESENDINTERVAL)
            {
                if (QueuedDamage > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Others, nameof(SendDamageEvent), QueuedDamage, QueuedWeaponType);
                    LastDamageSentTime = Time.time;
                    QueuedDamage = 0;
                }
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(sendQueuedDamage), DAMAGESENDINTERVAL);
            }
        }
        [System.NonSerialized] public float LastHitDamage;
        [System.NonSerialized] public byte LastHitWeaponType;
        [System.NonSerialized] public VRCPlayerApi LastHitByPlayer;
        [NetworkCallable]
        public void SendDamageEvent(float dmg, byte weaponType)
        {
            if (dead) return;
            LastHitByPlayer = NetworkCalling.CallingPlayer;
            LastHitDamage = dmg;
            LastHitWeaponType = weaponType;
            if (!localPlayer.IsOwner(gameObject)) return;
            Health = Mathf.Min(Health - dmg, FullHealth);
            if (SendKillEvents && Health <= 0 && Utilities.IsValid(LastHitByPlayer))
            {
                int killerID = LastHitByPlayer.playerId;
                byte killerWeaponType = LastHitWeaponType;
                if (killerID > -1 && !dead)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(KillEvent), killerID, weaponType); }
            }
            SendNetworkUpdate();
        }
        [NetworkCallable]
        public void KillEvent(int killerID, byte weaponType)
        {
            // this exists to tell the killer that they got a kill.
            if (killerID > -1)
            {
                VRCPlayerApi KillerAPI = VRCPlayerApi.GetPlayerById(killerID);
                if (Utilities.IsValid(KillerAPI))
                {
                    LastHitByPlayer = KillerAPI;
                    GameObject attackersVehicle = GameObject.Find(LastHitByPlayer.GetPlayerTag("SF_VehicleName"));
                    if (attackersVehicle)
                    {
                        LastAttacker = attackersVehicle.GetComponent<SaccEntity>();
                    }
                    else
                    {
                        LastAttacker = null;
                        return;
                    }
                }
                else
                {
                    LastHitByPlayer = null;
                    return;
                }
                LastHitWeaponType = weaponType;
                if (killerID == localPlayer.playerId)
                {
                    KillFeed.SetProgramVariable("useCustomKillMessage", true);
                    KillFeed.SetProgramVariable("KilledPlayerID", -2);
                    int MsgIndex = (byte)Random.Range(0, TargetKilledMessages.Length);
                    string killmessage = TargetKilledMessages[MsgIndex];
                    KillFeed.SetProgramVariable("MyKillMsg", killmessage);
                    KillFeed.SetProgramVariable("WeaponType", weaponType);
                    KillFeed.SendCustomEvent("sendKillMessage");
                    KillFeed.SetProgramVariable("useCustomKillMessage", false);
                }
            }
        }
        public void RespawnTarget()
        {
            if (!localPlayer.IsOwner(gameObject))
            { Networking.SetOwner(localPlayer, gameObject); }
            respawning = false;
            Health = FullHealth;
            SendNetworkUpdate();
        }
        bool respawning;
        public void SendNetworkUpdate()
        {
            if (Health <= 0)
            {
                Health = 0;
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
            Health = 0;
            SendNetworkUpdate();
        }
        float HPlast;
        bool deadlast;
        float PredictExplode_Time;
        void PredictExplode()
        {
            Health = 0;
            OnDeserialization();
            PredictExplode_Time = Time.time;
            SendCustomEventDelayedSeconds(nameof(ConfirmExploded), 1.01f);
        }
        public void ConfirmExploded()
        {
            if (Health == 0) return;
            else OnDeserialization();
        }
        public override void OnDeserialization()
        {
            if (Time.time - PredictExplode_Time < 1) return;
            lastUpdateTime = Time.time;
            if (Health < HPlast)
                TargetAnimator.SetTrigger("hit");
            dead = Health <= 0f;
            TargetAnimator.SetBool("dead", dead);
            TargetAnimator.SetFloat("healthpc", Health / FullHealth);
            if (dead && !deadlast)
            {
                foreach (UdonSharpBehaviour Exploder in ExplodeOther)
                {
                    if (Exploder)
                    {
                        Exploder.SendCustomEvent("Explode");
                    }
                }
            }
            HPlast = Health;
            deadlast = dead;
        }
    }
}