
using BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_CarBoost : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        [Tooltip("Amount of DriveSpeed added when boosting when not using BoostType_Force")]
        public float BoostAmount = 9900f;
        public float BoostInSeconds = 10f;
        public float ResupplyTime = 10f;
        public Animator BoostAnimator;
        public KeyCode BoostKey;
        private int BOOSTING_STRING = Animator.StringToHash("boosting");
        [FieldChangeCallback(nameof(BoostingAnimFloatName))] public string _BoostingAnimFloatName = "boosting";
        public string BoostingAnimFloatName
        {
            set
            {
                BOOSTING_STRING = Animator.StringToHash(value);
                _BoostingAnimFloatName = value;
            }
            get => _BoostingAnimFloatName;
        }
        private int BOOSTREMAINING_STRING;
        [FieldChangeCallback(nameof(BoostRemainingAnimFloatName))] public string _BoostRemainingAnimFloatName = "boostremaining";
        public string BoostRemainingAnimFloatName
        {
            set
            {
                BOOSTREMAINING_STRING = Animator.StringToHash(value);
                _BoostRemainingAnimFloatName = value;
            }
            get => _BoostRemainingAnimFloatName;
        }
        [Header("Use AddForce instead of increasing DriveSpeed?")]
        public bool BoostType_Force;
        [Tooltip("Amount of Force added when boosting using BoostType_Force")]
        public float BoostForce = 100f;
        public Transform BoostPoint;
        public bool UseMainFuel = false;
        [Tooltip("If using main fuel, use this much per second")]
        public float MainFuelUsePerSecond = 30f;
        [Header("Overheat mode:")]
        [Tooltip("Silly mode where you have infinite boost but your car explodes when it overheats")]
        public bool BoostOverheatMode;
        [Tooltip("Boost is recovered at this speed")]
        public float OverheatMode_replenishSpeed = 0.75f;
        [Header("Debug")]
        [UdonSynced, FieldChangeCallback(nameof(Boosting))] public float _Boosting;
        public float Boosting
        {
            set
            {
                BoostAnimator.SetFloat(BOOSTING_STRING, value);
                _Boosting = value;
            }
            get => _Boosting;
        }
        private float StartDriveSpeed;
        [UdonSynced, FieldChangeCallback(nameof(BoostRemaining))] public float _BoostRemaining;
        public float BoostRemaining
        {
            set
            {
                BoostAnimator.SetFloat(BOOSTREMAINING_STRING, value * BoostRemainingDivider);
                _BoostRemaining = value;
            }
            get => _BoostRemaining;
        }
        private Rigidbody VehicleRigidbody;
        private float BoostRemainingDivider;
        private float LastUpdateTime;
        private float RevLimiter;
        private bool Selected = false;
        private bool Piloting = false;
        private bool DoBoostRemainingAnim = false;
        private bool boostingLast = false;
        private bool ApplyBoostForce = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        public void SFEXT_L_EntityStart()
        {
            VehicleRigidbody = EntityControl.VehicleRigidbody;
            if (!BoostType_Force)
            {
                StartDriveSpeed = (float)SGVControl.GetProgramVariable("DriveSpeed");
                RevLimiter = (float)SGVControl.GetProgramVariable("RevLimiter");
            }
            BoostingAnimFloatName = _BoostingAnimFloatName;
            BoostRemainingAnimFloatName = _BoostRemainingAnimFloatName;
            BoostRemainingDivider = 1 / BoostInSeconds;
            if (UseMainFuel)
            { _BoostRemaining = 0; }
            else
            {
                if (_BoostRemainingAnimFloatName != string.Empty)//prevent missing parameter warning
                { BoostRemaining = BoostOverheatMode ? 0 : BoostInSeconds; }
                else
                { _BoostRemaining = BoostOverheatMode ? 0 : BoostInSeconds; }
            }
            Boosting = 0;
        }
        private void Update()
        {
            if (Piloting)
            {
                float Trigger = 0;
                if (Selected)
                {
                    if (LeftDial)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                }
                float BoostKeyb = Input.GetKey(BoostKey) ? 1f : 0f;
                float PilotBoosting;
                if (BoostOverheatMode)
                {
                    PilotBoosting = Mathf.Max(Trigger, BoostKeyb) > .75f ? 1 : 0;
                }
                else
                {
                    PilotBoosting = Mathf.Max(Trigger, BoostKeyb);
                }

                if (PilotBoosting > 0 && ((_BoostRemaining > 0 || BoostOverheatMode) || UseMainFuel && (float)SGVControl.GetProgramVariable("Fuel") > 0))
                {
                    boostingLast = true;
                    if (BoostType_Force)
                    {
                        Boosting = PilotBoosting;
                        ApplyBoostForce = true;
                    }
                    else
                    {
                        float engineSpeed = (float)SGVControl.GetProgramVariable("Revs") / RevLimiter;
                        Boosting = PilotBoosting * engineSpeed;
                        SGVControl.SetProgramVariable("DriveSpeed", StartDriveSpeed + (PilotBoosting * BoostAmount));
                        if (UseMainFuel)
                        {
                            SGVControl.SetProgramVariable("Fuel", (float)SGVControl.GetProgramVariable("Fuel") - (MainFuelUsePerSecond * Time.deltaTime * PilotBoosting * engineSpeed));
                        }
                        else
                        {
                            if (BoostOverheatMode)
                            {
                                if (BoostRemaining * BoostRemainingDivider >= 1)
                                {
                                    SGVControl.SendCustomEvent("NetworkExplode");
                                }
                                BoostRemaining += Time.deltaTime * PilotBoosting;
                            }
                            else
                            {
                                BoostRemaining -= Time.deltaTime * PilotBoosting * engineSpeed;
                            }
                        }
                    }
                    if (Time.time - LastUpdateTime > 0.3f)
                    {
                        RequestSerialization();
                    }
                }
                else if (boostingLast)
                {
                    cancelBoosting();
                }
                else if (BoostOverheatMode)
                {
                    BoostRemaining = Mathf.Max(BoostRemaining - (Time.deltaTime * OverheatMode_replenishSpeed), 0);
                }
            }
        }
        void cancelBoosting()
        {
            boostingLast = false;
            Boosting = 0;
            if (ApplyBoostForce)
            { ApplyBoostForce = false; }
            else
            { SGVControl.SetProgramVariable("DriveSpeed", StartDriveSpeed); }
            RequestSerialization();
        }
        public void SFEXT_G_RespawnButton()
        {
            Reset();
        }
        public void SFEXT_G_Explode()
        {
            Reset();
        }
        public void SFEXT_G_ReSupply()
        {
            if (UseMainFuel || BoostOverheatMode) { return; }
            if (_BoostRemaining != BoostInSeconds)
            {
                EntityControl.SetProgramVariable("ReSupplied", (int)EntityControl.GetProgramVariable("ReSupplied") + 1);
                if ((bool)SGVControl.GetProgramVariable("IsOwner"))
                {
                    BoostRemaining = Mathf.Min(_BoostRemaining + (BoostInSeconds / ResupplyTime), BoostInSeconds);
                    RequestSerialization();
                }
            }
        }
        public void SFEXT_G_ReFuel() { SFEXT_G_ReSupply(); }
        private void Reset()
        {
            Boosting = 0;
            if (BoostOverheatMode) { BoostRemaining = 0; }
            else
            { if (!UseMainFuel) { BoostRemaining = BoostInSeconds; } }
            if ((bool)SGVControl.GetProgramVariable("IsOwner"))
            {
                RequestSerialization();
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
            Boosting = 0;
            RequestSerialization();
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            Boosting = 0;
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            Boosting = 0;
        }
        public void SFEXT_O_PilotExit()
        {
            if (boostingLast)
            {
                cancelBoosting();
            }
            Piloting = false;
            Selected = false;
        }
        private void FixedUpdate()
        {
            if (BoostType_Force)
            {
                if (ApplyBoostForce)
                {
                    VehicleRigidbody.AddForceAtPosition(Boosting * BoostForce * BoostPoint.forward, BoostPoint.position, ForceMode.Acceleration);
                    if (UseMainFuel)
                    {
                        SGVControl.SetProgramVariable("Fuel", (float)SGVControl.GetProgramVariable("Fuel") - (MainFuelUsePerSecond * Time.deltaTime * Boosting));
                    }
                    else
                    {
                        if (BoostOverheatMode)
                        {
                            if (BoostRemaining * BoostRemainingDivider >= 1)
                            {
                                SGVControl.SendCustomEvent("NetworkExplode");
                            }
                        }
                        BoostRemaining -= Time.fixedDeltaTime * Boosting;
                    }
                }
                else if (BoostOverheatMode)
                {
                    BoostRemaining = Mathf.Max(BoostRemaining - (Time.deltaTime * OverheatMode_replenishSpeed), 0);
                }
            }
        }
    }
}