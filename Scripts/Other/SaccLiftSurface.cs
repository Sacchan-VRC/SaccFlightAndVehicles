
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccLiftSurface : UdonSharpBehaviour
    {
        public AnimationCurve PitchLift = AnimationCurve.Linear(1, 1, 1, 1);
        public AnimationCurve YawLift = AnimationCurve.Linear(1, 1, 1, 1);
        public Rigidbody VehicleRigidbody;
        // private Transform VehicleTransform;
        [Header("WingSize^2 and LiftStrength are multiplied together")]
        public float WingSize = 1;
        [Header("Make LiftStrength the same on all, then adjust WingSize visually")]
        public float LiftStrength = 0.001f;
        [Space(10)]
        public bool DoGroundEffect;
        public float GroundEffectRange;
        public float GroundEffectStrength;
        public bool DrawDebugGizmos = true;
        private VRCPlayerApi localPlayer;
        GameObject vehicle;
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            vehicle = VehicleRigidbody.gameObject;
        }
        private void FixedUpdate()
        {
            // if (!Networking.LocalPlayer.IsOwner(vehicle)) { return; }
            float AirDensity = 1;
            Vector3 vel = VehicleRigidbody.GetPointVelocity(transform.position);
            float speed = vel.magnitude * AirDensity;
            float AoA_Pitch = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(vel, transform.right), transform.right);
            float AoA_Yaw = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(vel, transform.up), transform.up);
            // float AoA_Yaw = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(vel, transform.up));
            float AoA_Pitch_LiftMulti = PitchLift.Evaluate(AoA_Pitch);
            float AoA_Yaw_LiftMulti = YawLift.Evaluate(AoA_Yaw);
            float liftForce = WingSize * WingSize * AoA_Pitch_LiftMulti * AoA_Yaw_LiftMulti * speed * speed * LiftStrength;
            Vector3 GroundEffect = Vector3.zero;
            if (DoGroundEffect)
            {
                bool Up = AoA_Pitch > 0;
                Vector3 GEDir = Up ? transform.up : -transform.up;
                RaycastHit GE;
                if (Physics.Raycast(transform.position, -GEDir, out GE, GroundEffectRange, 2065, QueryTriggerInteraction.Collide))
                {
                    GroundEffect = GEDir * (((-GE.distance + GroundEffectRange) / GroundEffectRange) * GroundEffectStrength * liftForce);
                }
#if UNITY_EDITOR
                GEDEBUGVEC = GroundEffect;
#endif
            }
            VehicleRigidbody.AddForceAtPosition(transform.up * liftForce + GroundEffect, transform.position, ForceMode.Acceleration);
#if UNITY_EDITOR
            liftdebug = liftForce;
            AOAYAWDEBUG = AoA_Yaw;
            AOAPITCHDEBUG = AoA_Pitch;
#endif
        }
#if UNITY_EDITOR
        public float AOAYAWDEBUG;
        public float AOAPITCHDEBUG;
        public Vector3 GEDEBUGVEC;
        public float liftdebug;
        public float DebugLineSize = 100f;
        public float DebugLineSizeGE = 100f;
        public float DebugLineSizeVEL = .1f;
        void OnDrawGizmos()
        {
            if (DrawDebugGizmos)
            {
                Gizmos.DrawRay(transform.position, VehicleRigidbody.velocity * DebugLineSizeVEL);

                Gizmos.DrawRay(transform.position, GEDEBUGVEC * DebugLineSizeGE);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(WingSize, .001f, WingSize));
                Gizmos.DrawRay(Vector3.zero, Vector3.up * liftdebug * DebugLineSize);
            }
        }
#endif
    }
}