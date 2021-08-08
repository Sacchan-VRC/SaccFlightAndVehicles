
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FloatScript : UdonSharpBehaviour
{
    [SerializeField] private Rigidbody VehicleRigidbody;
    [SerializeField] private Transform[] FloatPoints;
    [SerializeField] private LayerMask FloatLayers = 16;
    private Transform VehicleTransform;
    [SerializeField] private float FloatForce;
    [SerializeField] private float Compressing;
    [SerializeField] private float FloatRadius = .5f;
    [SerializeField] private float WaterSidewaysDrag = .1f;
    [SerializeField] private float WaterForwardDrag = .1f;
    [SerializeField] private float WaterRotDrag = .1f;
    [SerializeField] private float WaterVelDrag = .1f;
    [SerializeField] private float WaveHeight = .2f;
    [Tooltip("Size of wave noise pattern. Smaller Number = bigger pattern")]
    [SerializeField] private float WaveScale = 1;
    [Tooltip("How fast waves scroll across the sea")]
    [SerializeField] private float WaveSpeed = 1;
    [Tooltip("Try to float on the last (use this for hoverbikes)")]
    [SerializeField] private bool DoOnLand;
    [Tooltip("Automatically set multiple the relevent values by rigidbody weight, allowing the vehicle to be any weight without changing it's behaviour")]
    [SerializeField] bool AutoAdjustForWeight = true;

    [Header("HoverBike Only")]
    [SerializeField] private bool HoverBike = false;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private float HoverBikeTurningStrength = 1;
    [SerializeField] private float BackThrustStrength = 5;
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
    private float RBMass = 1;
    private float FloatDiameter;
    private int FPLength;
    void Start()
    {
        if (AutoAdjustForWeight)
        {
            RBMass = VehicleRigidbody.mass;
            FloatForce *= RBMass;
            WaterSidewaysDrag *= RBMass;
            WaterForwardDrag *= RBMass;
            WaterRotDrag *= RBMass;
            WaterVelDrag *= RBMass;
        }
        FPLength = FloatPoints.Length;
        FloatDiameter = FloatRadius * 2;

        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        { InEditor = true; }

        VehicleTransform = VehicleRigidbody.transform;

        FloatDepth = new float[FloatPoints.Length];
        FloatDepthLastFrame = new float[FloatPoints.Length];
        FloatLastRayHitHeight = new float[FloatPoints.Length];
        FloatTouchWaterPoint = new float[FloatPoints.Length];
        FloatPointForce = new Vector3[FloatPoints.Length];
        FloatLocalPos = new Vector3[FloatPoints.Length];
        HitLandLast = new bool[FloatPoints.Length];
        for (int i = 0; i != FloatPoints.Length; i++)
        {
            FloatLocalPos[i] = FloatPoints[i].localPosition;
        }
        if (HoverBike || (!InEditor && !localPlayer.isMaster))
        {
            gameObject.SetActive(false);
        }
    }
    public void SFEXT_O_TakeOwnership()
    {
        if (!HoverBike) { gameObject.SetActive(true); }
    }
    public void SFEXT_O_LoseOwnership()
    {
        if (!HoverBike) { gameObject.SetActive(false); }
    }
    public void SFEXT_O_PilotExit()
    {
        if (HoverBike) { gameObject.SetActive(false); }
    }
    public void SFEXT_O_PilotEnter()
    {
        if (HoverBike) { gameObject.SetActive(true); }
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!HoverBike && localPlayer.IsOwner(VehicleRigidbody.gameObject))
        { gameObject.SetActive(true); }
    }
    private void FixedUpdate()
    {
        Vector3 Vel = VehicleRigidbody.velocity;
        Vector3 RayCastPoint = FloatPoints[currentfloatpoint].position + (Vector3.up * FloatRadius);
        float TopOfFloat = RayCastPoint.y;
        Vector3 Waves = Vector3.zero;
        if (!HitLandLast[currentfloatpoint])//move RayCastPoint around to simulate waves
        {
            float time = Time.time;
            //add height of waves (noise(+-0.5) * waveheight)
            Waves = (Vector3.up * ((Mathf.PerlinNoise(((RayCastPoint.x + (time * WaveSpeed)) * WaveScale), ((RayCastPoint.z + (time * WaveSpeed)) * WaveScale)) * WaveHeight) - .5f));
            RayCastPoint += Waves;
        }
        RaycastHit hit;
        if (Physics.Raycast(RayCastPoint, -Vector3.up, out hit, 35, FloatLayers, QueryTriggerInteraction.Collide))
        {
            FloatTouchWaterPoint[currentfloatpoint] = hit.point.y + FloatDiameter + Waves.y;
            HitLandLast[currentfloatpoint] = !hit.collider.isTrigger;
            FloatLastRayHitHeight[currentfloatpoint] = hit.point.y;
        }
        else
        {
            FloatTouchWaterPoint[currentfloatpoint] = FloatLastRayHitHeight[currentfloatpoint] + FloatDiameter + Waves.y;
        }
        if (FloatTouchWaterPoint[currentfloatpoint] > TopOfFloat)
        {
            //Touching or under water
            if (DoOnLand || !HitLandLast[currentfloatpoint])
            {
                FloatDepth[currentfloatpoint] = FloatTouchWaterPoint[currentfloatpoint] - TopOfFloat;
                float CompressionDifference = ((FloatDepth[currentfloatpoint] - FloatDepthLastFrame[currentfloatpoint])) * RBMass;
                if (CompressionDifference > 0)
                { CompressionDifference *= Compressing; }
                else
                {
                    CompressionDifference = 0;
                }
                FloatDepthLastFrame[currentfloatpoint] = FloatDepth[currentfloatpoint];
                FloatPointForce[currentfloatpoint] = Vector3.up * (((FloatDepth[currentfloatpoint] * FloatForce) + CompressionDifference));
                //Debug.Log(string.Concat(currentfloatpoint.ToString(), ": floating: CompressonDif: ", CompressionDifference.ToString()));
                //Debug.Log(string.Concat(currentfloatpoint.ToString(), ": floating: floatpointforce: ", FloatPointForce[currentfloatpoint].ToString()));
            }
        }
        else
        {
            //In Air
            FloatDepth[currentfloatpoint] = 0;
            FloatDepthLastFrame[currentfloatpoint] = 0;
            FloatPointForce[currentfloatpoint] = Vector3.zero;
            if (Vel.y > 0)//only reset water level if moving up, so things don't break if we go straight from air all the way to under the water
            { FloatLastRayHitHeight[currentfloatpoint] = -500000; }
            //Debug.Log(string.Concat(currentfloatpoint.ToString(), ": Air: floatpointforce: ", FloatPointForce[currentfloatpoint].ToString()));
        }

        depth = 0;
        for (int i = 0; i != FloatDepth.Length; i++)
        {
            depth += FloatDepth[i];
        }
        if (depth > 0)
        {//apply last calculated floating force for each floatpoint to respective floatpoints
            for (int i = 0; i != FloatPoints.Length; i++)
            {
                VehicleRigidbody.AddForceAtPosition(FloatPointForce[i], FloatPoints[i].position, ForceMode.Force);
            }
            VehicleRigidbody.AddTorque(-VehicleRigidbody.angularVelocity * depth * WaterRotDrag);
            VehicleRigidbody.AddForce(-VehicleRigidbody.velocity * depth * WaterVelDrag);
        }

        Vector3 right = VehicleTransform.right;
        Vector3 forward = VehicleTransform.forward;

        float sidespeed = Vector3.Dot(Vel, right);
        float forwardspeed = Vector3.Dot(Vel, forward);

        if (HoverBike)
        {
            Vector3 up = VehicleTransform.up;
            float RightY = Mathf.Abs(right.y);
            right.y = 0;
            if (Vector3.Dot(Vel, -up) > 0)
            {
                right = right.normalized * (1 + (RightY * HoverBikeTurningStrength));
            }
            else
            {
                right = Vector3.zero;
            }
            float BackThrustAmount = -((Vector3.Dot(Vel, forward)) * BackThrustStrength);
            if (BackThrustAmount > 0)
            { VehicleRigidbody.AddForce(forward * BackThrustAmount * depth * EngineControl.ThrottleInput); }
            VehicleRigidbody.AddForce(right * -sidespeed * WaterSidewaysDrag * depth, ForceMode.Force);
        }
        else
        {
            VehicleRigidbody.AddForceAtPosition(right * -sidespeed * WaterSidewaysDrag * depth, FloatPoints[currentfloatpoint].position, ForceMode.Force);
        }
        VehicleRigidbody.AddForceAtPosition(forward * -forwardspeed * WaterForwardDrag * depth, FloatPoints[currentfloatpoint].position, ForceMode.Force);

        currentfloatpoint++;
        if (currentfloatpoint == FPLength) { currentfloatpoint = 0; }
    }
}
