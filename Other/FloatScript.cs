
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
    [SerializeField] private bool DoOnLand;
    [SerializeField] private float SuspMaxDist = .5f;
    [SerializeField] private float WaterSidewaysDrag = .1f;
    [SerializeField] private float WaterForwardDrag = .1f;
    [SerializeField] private float WaterRotDrag = .1f;
    [SerializeField] private float WaterVelDrag = .1f;
    [SerializeField] private float WaveHeight = .2f;
    [SerializeField] private float WaveScale = 1;
    [SerializeField] private float WaveSpeed = 1;

    [Header("HoverBike Only")]
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private bool HoverBike = false;
    [SerializeField] private float HoverBikeTurningStrength = 1;
    [SerializeField] private float BackThrustStrength = 5;
    private float[] SuspensionCompression;
    private float[] SuspensionCompressionLastFrame;
    private float[] FloatPointHeightLastFrame;
    private float[] DepthBeyondMaxSusp;
    private Vector3[] FloatLocalPos;
    private float LastRayHitHeight = float.MinValue;
    private Vector3[] FloatPointForce;
    private int currentfloatpoint;
    [System.NonSerializedAttribute] public float depth;
    float SuspDispToMeters;
    private VRCPlayerApi localPlayer;
    private bool InEditor = false;
    private bool HitLandLast;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        { InEditor = true; }

        SuspDispToMeters = 1 / SuspMaxDist;

        VehicleTransform = VehicleRigidbody.transform;

        SuspensionCompression = new float[FloatPoints.Length];
        SuspensionCompressionLastFrame = new float[FloatPoints.Length];
        FloatPointHeightLastFrame = new float[FloatPoints.Length];
        DepthBeyondMaxSusp = new float[FloatPoints.Length];
        FloatPointForce = new Vector3[FloatPoints.Length];
        FloatLocalPos = new Vector3[FloatPoints.Length];
        for (int i = 0; i != FloatPoints.Length; i++)
        {
            FloatLocalPos[i] = FloatPoints[i].localPosition;
        }
        if (HoverBike || (!InEditor && !localPlayer.isMaster))
        {
            gameObject.SetActive(false);
        }
    }
    private void OnEnable()
    {
        LastRayHitHeight = float.MinValue;
    }
    public void TakeOwnership()
    {
        if (!HoverBike) { gameObject.SetActive(true); }
    }
    public void LoseOwnership()
    {
        if (!HoverBike) { gameObject.SetActive(false); }
    }
    public void PilotExit()
    {
        if (HoverBike) { gameObject.SetActive(false); }
    }
    public void PilotEnter()
    {
        if (HoverBike) { gameObject.SetActive(true); }
    }
    private void FixedUpdate()
    {
        //fire test ray to check if it's worth firing another ray, and to check the height of the water/ground surface and if it's water or land
        if (LastRayHitHeight < transform.position.y)
        {
            RaycastHit hit;
            //15 meters should be far enough for this to work with any size of plane, needs to be increased if you're trying to make something gigantic float
            if (Physics.Raycast(transform.position, -Vector3.up, out hit, 15, FloatLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.isTrigger)//trigger = water
                {
                    HitLandLast = false;
                }
                else
                {
                    HitLandLast = true;
                }

                if (DoOnLand || !HitLandLast)
                {
                    LastRayHitHeight = hit.point.y;
                }
            }
            else
            {
                LastRayHitHeight = float.MinValue;
            }
        }
        //if the water/ground level found above is higher than the current floatpoint, values are based on how far underneath the water it is rather than using a raycast to it
        if (LastRayHitHeight > FloatPoints[currentfloatpoint].position.y)
        {
            DepthBeyondMaxSusp[currentfloatpoint] = LastRayHitHeight - FloatPoints[currentfloatpoint].position.y;
            SuspensionCompression[currentfloatpoint] = 1;
            SuspensionCompressionLastFrame[currentfloatpoint] = 1;

            float CompressionDifference = FloatPoints[currentfloatpoint].position.y - FloatPointHeightLastFrame[currentfloatpoint];
            if (CompressionDifference < 0)
            { CompressionDifference *= -Compressing; }
            FloatPointForce[currentfloatpoint] = Vector3.up * (FloatForce + (CompressionDifference * SuspDispToMeters));
            FloatPointHeightLastFrame[currentfloatpoint] = FloatPoints[currentfloatpoint].position.y;
        }
        else//check depth and calc values for one floatpoint per frame
        {
            DepthBeyondMaxSusp[currentfloatpoint] = 0;
            if (!HitLandLast)//move floats around to simulate waves
            {
                Vector3 floatpos = FloatPoints[currentfloatpoint].position;
                float time = Time.time;
                FloatPoints[currentfloatpoint].localPosition = new Vector3(FloatLocalPos[currentfloatpoint].x, FloatLocalPos[currentfloatpoint].y - (Mathf.PerlinNoise(((floatpos.x + (time * WaveSpeed)) * WaveScale), ((floatpos.z + (time * WaveSpeed)) * WaveScale)) * WaveHeight), FloatLocalPos[currentfloatpoint].z);
            }
            else//we're a hoverbike-like vehicle that is currently over land, no waves
            {
                FloatPoints[currentfloatpoint].localPosition = FloatLocalPos[currentfloatpoint];
            }
            RaycastHit hit;
            if (Physics.Raycast(FloatPoints[currentfloatpoint].position, -Vector3.up, out hit, SuspMaxDist, FloatLayers, QueryTriggerInteraction.Collide))
            {
                if (DoOnLand || hit.collider.isTrigger)
                {
                    SuspensionCompression[currentfloatpoint] = Mathf.Clamp(((hit.distance / SuspMaxDist) * -1) + 1, 0, 1);
                    float CompressionDifference = (SuspensionCompression[currentfloatpoint] - SuspensionCompressionLastFrame[currentfloatpoint]);
                    if (CompressionDifference > 0)
                    { CompressionDifference *= Compressing; }

                    SuspensionCompressionLastFrame[currentfloatpoint] = SuspensionCompression[currentfloatpoint];
                    FloatPointForce[currentfloatpoint] = Vector3.up * (((SuspensionCompression[currentfloatpoint] * FloatForce) + CompressionDifference));
                }
            }
            else
            {
                SuspensionCompression[currentfloatpoint] = 0;
                FloatPointForce[currentfloatpoint] = Vector3.zero;
            }
        }
        //set float back to it's original position before adding forces
        FloatPoints[currentfloatpoint].localPosition = FloatLocalPos[currentfloatpoint];

        depth = 0;
        for (int i = 0; i != SuspensionCompression.Length; i++)
        {
            depth += SuspensionCompression[i] + (DepthBeyondMaxSusp[i] * SuspDispToMeters);
        }
        if (depth > 0)
        {//apply last calculated floating force to all floatpoints
            for (int i = 0; i != FloatPoints.Length; i++)
            {
                VehicleRigidbody.AddForceAtPosition(FloatPointForce[i], FloatPoints[i].position, ForceMode.Force);
            }
            VehicleRigidbody.AddTorque(-VehicleRigidbody.angularVelocity * depth * WaterRotDrag);
            VehicleRigidbody.AddForce(-VehicleRigidbody.velocity * depth * WaterVelDrag);
        }

        Vector3 Vel = VehicleRigidbody.velocity;
        Vector3 right = VehicleTransform.right;
        Vector3 forward = VehicleTransform.forward;
        Vector3 up = VehicleTransform.up;

        float sidespeed = Vector3.Dot(Vel, right);
        float forwardspeed = Vector3.Dot(Vel, forward);

        if (HoverBike)
        {
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
        if (currentfloatpoint == FloatPoints.Length) { currentfloatpoint = 0; }
    }
}
