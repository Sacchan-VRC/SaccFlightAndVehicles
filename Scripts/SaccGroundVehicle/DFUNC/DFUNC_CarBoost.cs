
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
        [Header("Use AddForce instead of increasing DriveSpeed?")]
        public bool BoostType_Force;
        [Tooltip("Amount of Force added when boosting using BoostType_Force")]
        public float BoostForce = 100f;
        public Transform BoostPoint;
        private Rigidbody VehicleRigidbody;
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
        private float BoostRemainingDivider;
        private float LastUpdateTime;
        private float RevLimiter;
        private bool Selected = false;
        private bool Piloting = false;
        private bool boostingLast = false;
        private bool UseLeftTrigger = false;
        private bool ApplyBoostForce = false;
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
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        void Start()
        {
            VehicleRigidbody = (Rigidbody)SGVControl.GetProgramVariable("VehicleRigidbody");
            StartDriveSpeed = (float)SGVControl.GetProgramVariable("DriveSpeed");
            RevLimiter = (float)SGVControl.GetProgramVariable("RevLimiter");
            BoostRemaining = BoostInSeconds;
            BoostRemainingDivider = 1 / BoostInSeconds;
            BoostRemainingAnimFloatName = _BoostRemainingAnimFloatName;
            BoostingAnimFloatName = _BoostingAnimFloatName;
            BoostAnimator.SetFloat(BOOSTING_STRING, 0);
            BoostAnimator.SetFloat(BOOSTREMAINING_STRING, 1);
        }
        private void Update()
        {
            if (Piloting)
            {
                float Trigger = 0;
                if (Selected)
                {
                    if (UseLeftTrigger)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                }
                float BoostKeyb = Input.GetKey(BoostKey) ? 1f : 0f;
                float PilotBoosting = Mathf.Max(Trigger, BoostKeyb);

                if (PilotBoosting > 0 && BoostRemaining > 0)
                {
                    if (BoostType_Force)
                    {
                        ApplyBoostForce = true;
                    }
                    else
                    {
                        SGVControl.SetProgramVariable("DriveSpeed", StartDriveSpeed + (PilotBoosting * BoostAmount));
                        BoostRemaining -= Time.deltaTime * PilotBoosting * (float)SGVControl.GetProgramVariable("Revs") / RevLimiter;
                    }
                    boostingLast = true;
                    Boosting = PilotBoosting;
                    if (Time.time - LastUpdateTime > 0.3f)
                    {
                        RequestSerialization();
                    }
                }
                else if (boostingLast)
                {
                    RequestSerialization();
                    Boosting = 0;
                    SGVControl.SetProgramVariable("DriveSpeed", StartDriveSpeed);
                    boostingLast = false;
                    ApplyBoostForce = false;
                }
            }
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
            if (BoostRemaining != BoostInSeconds)
            {
                SGVControl.SetProgramVariable("ReSupplied", (int)SGVControl.GetProgramVariable("ReSupplied") + 1);
                if ((bool)SGVControl.GetProgramVariable("IsOwner"))
                {
                    BoostRemaining = Mathf.Min(BoostRemaining + (BoostInSeconds / ResupplyTime), BoostInSeconds);
                    RequestSerialization();
                }
            }
        }
        private void Reset()
        {
            Boosting = 0;
            BoostRemaining = BoostInSeconds;
            if ((bool)SGVControl.GetProgramVariable("IsOwner"))
            {
                RequestSerialization();
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
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
            boostingLast = false;
            Boosting = 0;
            ApplyBoostForce = false;
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
        }
        private void FixedUpdate()
        {
            if (ApplyBoostForce)
            {
                VehicleRigidbody.AddForceAtPosition(Boosting * BoostForce * BoostPoint.forward, BoostPoint.position, ForceMode.Acceleration);
                BoostRemaining -= Time.deltaTime * Boosting;
            }
        }
    }
}