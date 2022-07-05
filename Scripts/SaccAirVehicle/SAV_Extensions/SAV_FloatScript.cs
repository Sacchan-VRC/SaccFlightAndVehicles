
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_FloatScript : UdonSharpBehaviour
    {
        public Rigidbody VehicleRigidbody;
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Transforms at which floating forces are calculate, recomd using 4 in a rectangle centered around the center of mass")]
        public Transform[] FloatPoints;
        [Tooltip("Layers to raycast against to check for 'water'")]
        public LayerMask FloatLayers = 16;
        private Transform VehicleTransform;
        [Tooltip("Multiplier for the forces pushing up")]
        public float FloatForce = 5;
        [Tooltip("Multiplier for the forces pushing up when dead, set lower to make vehicle sink")]
        public float DeadFloatForce = 5;
        private float _floatForce;
        [Tooltip("Max possible value to increase force by based on depth. Prevent objects from moving way too fast if dragged to the bottom of the water")]
        public float MaxDepthForce = 25;
        [Tooltip("Set a lower max depth force value to make the vehicle sink when dead")]
        public float DeadMaxDepthForce = .4f;
        private float _maxDepthForce;
        [Tooltip("Value that the floating forces are multiplied by while vehicle is moving down in water. Higher = more stable floating")]
        public float Compressing = 25;
        [Tooltip("Prevent extra force from compression becoming too high if the object is teleported deep underwater")]
        public float MaxCompressingForce = 25;
        [Tooltip("Physical size of the simulated spherical float in meters")]
        public float FloatRadius = .6f;
        [Tooltip("Strength of drag force applied by perpendicular movement in water (applied at floats)")]
        public float WaterSidewaysDrag = 1f;
        [Tooltip("Strength of drag force applied by forward movement in water (applied at floats)")]
        public float WaterForwardDrag = .05f;
        [Tooltip("Strength of force slowing rotation in water")]
        public float WaterRotDrag = 5;
        [Tooltip("Strength of drag force slowing down the vehicle (applied at center of mass, causes no rotation)")]
        public float WaterVelDrag = 0f;
        public float WaveHeight = .6f;
        [Tooltip("Size of wave noise pattern. Smaller Number = bigger pattern")]
        public float WaveScale = .04f;
        [Tooltip("How fast waves scroll across the sea")]
        public float WaveSpeed = 12;
        [Tooltip("How high above the last hit surface the raycast starts from. Needed for large waves or any non-solid hoverable surface")]

        [Range(1f, 19)]
        public float RayCastHeight = 2;
        [Tooltip("'Float' on solid objects (non-trigger) (used by hoverbikes)")]
        public bool DoOnLand = false;
        [Tooltip("Disable the totally non-physical ground rotation functionality")]
        public bool DisableTaxiRotation = false;
        [Header("HoverBike Only")]
        [Tooltip("If hoverbike, script is only active when being piloted, also adds steering effects when near the ground")]
        public bool HoverBike = false;
        [Tooltip("If hoverbike, there are some 'unrealistic' turning physics when near the ground. This multiplies the strength of the rolling-into-a-turn extra turning ability")]
        public float HoverBikeTurningStrength = .2f;
        [Tooltip("If hoverbike, there are some 'unrealistic' turning physics when near the ground. This multiplies the strength of the drifing-at-90-degrees extra turning ability")]
        public float BackThrustStrength = 15;
        [System.NonSerializedAttribute] public float SurfaceHeight;
        private float[] FloatDepth;
        private float[] FloatDepthLastFrame;
        private float[] FloatLastRayHitHeight;
        private float[] FloatTouchWaterPoint;
        private bool[] HitLandLast;
        private Vector3[] FloatLocalPos;
        private Vector3[] FloatPointForce;
        private int currentfloatpoint;
        [System.NonSerializedAttribute] public float depth;
        private VRCPlayerApi localPlayer;
        private bool InEditor = false;
        private float FloatDiameter;
        private int FPLength;
        void Start()
        {
            if (!SAVControl)
            { SFEXT_L_EntityStart(); }
        }
        public void SFEXT_L_EntityStart()
        {
            FPLength = FloatPoints.Length;
            FloatDiameter = FloatRadius * 2;
            _maxDepthForce = MaxDepthForce * 90;
            _floatForce = FloatForce;

            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            { InEditor = true; }

            VehicleTransform = VehicleRigidbody.transform;

            int numfloats = FloatPoints.Length;
            FloatDepth = new float[numfloats];
            FloatDepthLastFrame = new float[numfloats];
            FloatLastRayHitHeight = new float[numfloats];
            FloatTouchWaterPoint = new float[numfloats];
            FloatPointForce = new Vector3[numfloats];
            FloatLocalPos = new Vector3[numfloats];
            HitLandLast = new bool[numfloats];
            for (int i = 0; i != numfloats; i++)
            {
                FloatLocalPos[i] = FloatPoints[i].localPosition;
                FloatTouchWaterPoint[i] = float.MinValue;
                FloatLastRayHitHeight[i] = float.MinValue;
            }
            if (!HoverBike && (InEditor || localPlayer.isMaster))
            {
                gameObject.SetActive(true);
            }
            if (HoverBike || DisableTaxiRotation)
            {
                SAVControl.SetProgramVariable("DisableTaxiRotation", (int)SAVControl.GetProgramVariable("DisableTaxiRotation") + 1);
                SAVControl.SetProgramVariable("Taxiing", false);
            }
        }
        public void SFEXT_O_TakeOwnership()
        {
            if (!HoverBike || (HoverBike && (bool)SAVControl.GetProgramVariable("_EngineOn"))) { gameObject.SetActive(true); }
            InitializeDepth();
        }
        public void SFEXT_O_LoseOwnership()
        {
            gameObject.SetActive(false);
        }
        public void SFEXT_O_Explode()
        {
            _maxDepthForce = DeadMaxDepthForce * 90;
            _floatForce = DeadFloatForce;
        }
        public void SFEXT_G_ReAppear()
        {
            _maxDepthForce = MaxDepthForce * 90;
            _floatForce = FloatForce;
        }
        public void SFEXT_G_EngineOff()
        {
            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
            {
                if (HoverBike) { gameObject.SetActive(false); }
            }
        }
        public void SFEXT_G_EngineOn()
        {
            if (HoverBike && ((bool)SAVControl.GetProgramVariable("Piloting")))
            {
                gameObject.SetActive(true);
                for (int i = 0; i != FloatPoints.Length; i++)
                {
                    FloatTouchWaterPoint[i] = float.MinValue;
                    FloatLastRayHitHeight[i] = float.MinValue;
                }
            }
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!HoverBike && localPlayer.IsOwner(VehicleRigidbody.gameObject))
            { gameObject.SetActive(true); }
        }
        public void InitializeDepth()
        {
            //local client doesn't have height of water so they need to find it when taking ownership
            for (int i = 0; i < FPLength; i++)
            {
                FindDepth(i);
            }
        }
        public void FindDepth(int i)
        {
            //find water level
            RaycastHit checkhit;
            if (Physics.Raycast(FloatPoints[i].position, Vector3.up, out checkhit, Mathf.Infinity, FloatLayers, QueryTriggerInteraction.Collide))
            {
                if (Physics.Raycast(checkhit.point, -Vector3.up, out checkhit, Mathf.Infinity, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    FloatLastRayHitHeight[i] = checkhit.point.y;
                    HitLandLast[i] = !checkhit.collider.isTrigger;
                }
                else
                { FloatLastRayHitHeight[i] = float.MinValue; }
            }
            else
            {
                if (Physics.Raycast(FloatPoints[i].position + (Vector3.up * 100f), -Vector3.up, out checkhit, Mathf.Infinity, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    FloatLastRayHitHeight[i] = checkhit.point.y;
                    HitLandLast[i] = !checkhit.collider.isTrigger;
                }
                else
                { FloatLastRayHitHeight[i] = float.MinValue; }
            }


            //rest is same as fixedupdate with some stuff removed
            Vector3 Vel = VehicleRigidbody.velocity;
            Vector3 TopOfFloat = FloatPoints[i].position + (Vector3.up * FloatRadius);
            Vector3 Waves = Vector3.zero;
            if (!HitLandLast[i])
            {
                float time = Time.time;
                Waves.y = ((Mathf.PerlinNoise(((TopOfFloat.x + (time * WaveSpeed)) * WaveScale), ((TopOfFloat.z + (time * WaveSpeed)) * WaveScale)) * WaveHeight) - .5f);
            }

            RaycastHit hit;
            FloatTouchWaterPoint[i] = FloatLastRayHitHeight[i] + FloatDiameter + Waves.y;
            if (FloatTouchWaterPoint[i] > TopOfFloat.y && (DoOnLand || !HitLandLast[i]))
            {
                FloatDepth[i] = FloatTouchWaterPoint[i] - TopOfFloat.y;

                FloatDepthLastFrame[i] = FloatDepth[i];
                FloatPointForce[currentfloatpoint] = Vector3.up * Time.deltaTime * (((Mathf.Min(FloatDepth[currentfloatpoint], _maxDepthForce * Time.deltaTime) * _floatForce)));
                Vector3 checksurface = new Vector3(TopOfFloat.x, FloatLastRayHitHeight[i] + RayCastHeight, TopOfFloat.z);
                if (Physics.Raycast(checksurface, -Vector3.up, out hit, 20, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    if (DoOnLand || hit.collider.isTrigger)
                    { SurfaceHeight = FloatLastRayHitHeight[i] = hit.point.y; }
                }
                else
                {
                    FloatLastRayHitHeight[i] = float.MinValue;
                }
            }
            else
            {
                //In Air
                FloatDepth[i] = 0;
                FloatDepthLastFrame[i] = 0;
                if (Vel.y > 0 || HitLandLast[i])
                { FloatTouchWaterPoint[i] = float.MinValue; }
                Vector3 checksurface = new Vector3(TopOfFloat.x, TopOfFloat.y + WaveHeight, TopOfFloat.z);
                if (Physics.Raycast(TopOfFloat, -Vector3.up, out hit, 35, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    FloatTouchWaterPoint[i] = hit.point.y + FloatDiameter + Waves.y;
                    FloatLastRayHitHeight[i] = hit.point.y;
                }
                else
                {
                    FloatLastRayHitHeight[i] = float.MinValue;
                }
            }
        }
        private void FixedUpdate()
        {
            Vector3 Vel = VehicleRigidbody.velocity;
            Vector3 TopOfFloat = FloatPoints[currentfloatpoint].position + (Vector3.up * FloatRadius);
            Vector3 Waves = Vector3.zero;
            if (!HitLandLast[currentfloatpoint])
            {
                float time = Time.time;
                //waves = (noise(+-0.5) * waveheight)
                Waves.y = ((Mathf.PerlinNoise(((TopOfFloat.x + (time * WaveSpeed)) * WaveScale), ((TopOfFloat.z + (time * WaveSpeed)) * WaveScale)) * WaveHeight) - .5f);
            }
            ///if above water, trace down to find water
            //if touching/in water trace down from diameter above last water height at current xz to find water
            RaycastHit hit;
            FloatTouchWaterPoint[currentfloatpoint] = FloatLastRayHitHeight[currentfloatpoint] + FloatDiameter + Waves.y;
            if (FloatTouchWaterPoint[currentfloatpoint] > TopOfFloat.y && (DoOnLand || !HitLandLast[currentfloatpoint]))
            {
                FloatDepth[currentfloatpoint] = (FloatTouchWaterPoint[currentfloatpoint] - TopOfFloat.y);
                float CompressionDifference = ((FloatDepth[currentfloatpoint] - FloatDepthLastFrame[currentfloatpoint]));
                if (CompressionDifference > 0)
                { CompressionDifference = Mathf.Min(CompressionDifference * Compressing, MaxCompressingForce); }
                else
                {
                    CompressionDifference = 0;
                }
                FloatDepthLastFrame[currentfloatpoint] = FloatDepth[currentfloatpoint];
                FloatPointForce[currentfloatpoint] = Vector3.up * Time.deltaTime * (((Mathf.Min(FloatDepth[currentfloatpoint], _maxDepthForce * Time.deltaTime) * _floatForce) + (CompressionDifference / Time.deltaTime / 90)));
                //float is potentially below the top of the trigger, so fire a raycast from above the last known trigger height to check if it's still there
                //the '+10': larger number means less chance of error if moving faster on a sloped water trigger, but could cause issues with bridges etc
                Vector3 checksurface = new Vector3(TopOfFloat.x, FloatLastRayHitHeight[currentfloatpoint] + RayCastHeight, TopOfFloat.z);
                if (Physics.Raycast(checksurface, -Vector3.up, out hit, 20, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    if (DoOnLand || hit.collider.isTrigger)
                    { SurfaceHeight = FloatLastRayHitHeight[currentfloatpoint] = hit.point.y; }
                }
                else
                {
                    FloatLastRayHitHeight[currentfloatpoint] = float.MinValue;
                }
            }
            else
            {
                //In Air
                FloatDepth[currentfloatpoint] = 0;
                FloatDepthLastFrame[currentfloatpoint] = 0;
                FloatPointForce[currentfloatpoint] = Vector3.zero;
                if (Vel.y > 0 || HitLandLast[currentfloatpoint])//only reset water level if moving up (or last hit was land), so things don't break if we go straight from air all the way to under the water
                { FloatTouchWaterPoint[currentfloatpoint] = float.MinValue; }
                //Debug.Log(string.Concat(currentfloatpoint.ToString(), ": Air: floatpointforce: ", FloatPointForce[currentfloatpoint].ToString()));
                //float could be below the top of the trigger if the waves are big enough, check for water trigger with current position + waveheight
                Vector3 checksurface = new Vector3(TopOfFloat.x, TopOfFloat.y + WaveHeight, TopOfFloat.z);
                if (Physics.Raycast(TopOfFloat, -Vector3.up, out hit, 35, FloatLayers, QueryTriggerInteraction.Collide))
                {
                    FloatTouchWaterPoint[currentfloatpoint] = hit.point.y + FloatDiameter + Waves.y; ;
                    HitLandLast[currentfloatpoint] = !hit.collider.isTrigger;
                    FloatLastRayHitHeight[currentfloatpoint] = hit.point.y;
                }
                else
                {
                    FloatLastRayHitHeight[currentfloatpoint] = float.MinValue;
                }
            }

            depth = 0;
            for (int i = 0; i != FPLength; i++)
            {
                depth += FloatDepth[i];
            }
            float DepthMaxd = Mathf.Min(depth, _maxDepthForce * Time.deltaTime);
            if (depth > 0)
            {//apply last calculated floating force for each floatpoint to respective floatpoint
                for (int i = 0; i != FloatPoints.Length; i++)
                {
                    VehicleRigidbody.AddForceAtPosition(FloatPointForce[i], FloatPoints[i].position, ForceMode.VelocityChange);
                }
                VehicleRigidbody.AddTorque(-VehicleRigidbody.angularVelocity * DepthMaxd * WaterRotDrag, ForceMode.Acceleration);
                VehicleRigidbody.AddForce(-VehicleRigidbody.velocity * DepthMaxd * WaterVelDrag, ForceMode.Acceleration);
                if (SAVControl && !HoverBike) { SAVControl.SetProgramVariable("Floating", true); }


                Vector3 right = VehicleTransform.right;
                Vector3 forward = VehicleTransform.forward;

                float sidespeed = Vector3.Dot(Vel, right);
                float forwardspeed = Vector3.Dot(Vel, forward);
                if (HoverBike)
                {
                    Vector3 hoverup = Vector3.Cross(Vel, forward);
                    Vector3 up = VehicleTransform.up;
                    float RightY = Mathf.Abs(Vector3.Dot(hoverup, right));
                    right = Vector3.ProjectOnPlane(right, hoverup);
                    if (Vector3.Dot(Vel, -up) > 0)//rolling into turn?
                    {
                        right = right.normalized * ((RightY * HoverBikeTurningStrength));
                        VehicleRigidbody.AddForce(right * -sidespeed * DepthMaxd, ForceMode.Acceleration);
                    }
                    float BackThrustAmount = -((Vector3.Dot(Vel, forward)) * BackThrustStrength);
                    if (BackThrustAmount > 0)
                    { VehicleRigidbody.AddForce(forward * BackThrustAmount * DepthMaxd * (float)SAVControl.GetProgramVariable("ThrottleInput"), ForceMode.Acceleration); }
                }
                else
                {
                    VehicleRigidbody.AddForceAtPosition(right * -sidespeed * WaterSidewaysDrag * DepthMaxd, FloatPoints[currentfloatpoint].position, ForceMode.Acceleration);
                }
                VehicleRigidbody.AddForceAtPosition(forward * -forwardspeed * WaterForwardDrag * DepthMaxd, FloatPoints[currentfloatpoint].position, ForceMode.Acceleration);

            }
            else
            { if (SAVControl && !HoverBike) { SAVControl.SetProgramVariable("Floating", false); } }

            currentfloatpoint++;
            if (currentfloatpoint == FPLength) { currentfloatpoint = 0; }
        }
    }
}
