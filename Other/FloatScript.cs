
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FloatScript : UdonSharpBehaviour
{
    [SerializeField] private Rigidbody VehicleRigidbody;
    private Transform VehicleTransform;
    [SerializeField] private float Compressing;
    [SerializeField] private float Rebound;
    [SerializeField] private float FloatForce;
    [SerializeField] private bool DoOnLand;
    [SerializeField] private Transform[] FloatPoints;
    [SerializeField] private float SuspMaxDist = .5f;
    [SerializeField] private float WaterSidewaysDrag = .1f;
    [SerializeField] private float WaterForwardDrag = .1f;
    [SerializeField] private float WaterRotDrag = .1f;
    [SerializeField] private float WaterVelDrag = .1f;
    private float[] SuspensionCompression;
    private float[] SuspensionCompressionLastFrame;
    private float[] FloatPointHeightLastFrame;
    private float[] DepthBeyondMaxSusp;
    private float LastRayHitHeight = -99999999;
    private Vector3[] FloatPointForce;
    private int currentfloatpoint;
    private float depth;
    float SuspDispToMeters;
    private VRCPlayerApi localPlayer;
    private bool InEditor = false;
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
    }
    void FixedUpdate()
    {
        if (InEditor || localPlayer.IsOwner(gameObject))
        {
            Floating();
        }
    }
    private void Floating()
    {
        if (LastRayHitHeight < transform.position.y)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit, 5, 1, QueryTriggerInteraction.Collide))
            {
                if (DoOnLand || hit.collider.isTrigger)
                {
                    LastRayHitHeight = hit.point.y;

                }
            }
            else
            {
                LastRayHitHeight = float.MinValue;
            }
        }


        if (LastRayHitHeight > FloatPoints[currentfloatpoint].position.y)
        {
            DepthBeyondMaxSusp[currentfloatpoint] = LastRayHitHeight - FloatPoints[currentfloatpoint].position.y;
            //float is under da wata
            SuspensionCompression[currentfloatpoint] = 1;
            SuspensionCompressionLastFrame[currentfloatpoint] = 1;

            float CompressionDifference = FloatPoints[currentfloatpoint].position.y - FloatPointHeightLastFrame[currentfloatpoint];
            if (CompressionDifference < 0)
            { CompressionDifference *= -Compressing; }
            else
            { CompressionDifference *= -Rebound; }
            FloatPointForce[currentfloatpoint] = Vector3.up * (FloatForce + (CompressionDifference * SuspDispToMeters));
            FloatPointHeightLastFrame[currentfloatpoint] = FloatPoints[currentfloatpoint].position.y;
        }
        else
        {
            DepthBeyondMaxSusp[currentfloatpoint] = 0;
            //check depth of one floatpoint per frame
            RaycastHit hit;
            if (Physics.Raycast(FloatPoints[currentfloatpoint].position, -Vector3.up, out hit, SuspMaxDist, 1, QueryTriggerInteraction.Collide))
            {
                if (DoOnLand || hit.collider.isTrigger)
                {
                    SuspensionCompression[currentfloatpoint] = Mathf.Clamp(((hit.distance / SuspMaxDist) * -1) + 1, 0, 1);
                    float CompressionDifference = (SuspensionCompression[currentfloatpoint] - SuspensionCompressionLastFrame[currentfloatpoint]);
                    if (CompressionDifference > 0)
                    { CompressionDifference *= Compressing; }
                    else
                    { CompressionDifference *= Rebound; }

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
        depth = 0;
        for (int i = 0; i != SuspensionCompression.Length; i++)
        {
            depth += SuspensionCompression[i] + DepthBeyondMaxSusp[i];
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
        float sidespeed = Vector3.Dot(Vel, VehicleTransform.right);
        float forwardspeed = Vector3.Dot(Vel, VehicleTransform.forward);

        VehicleRigidbody.AddForceAtPosition(VehicleTransform.right * -sidespeed * WaterSidewaysDrag * depth, FloatPoints[currentfloatpoint].position, ForceMode.Force);
        VehicleRigidbody.AddForceAtPosition(VehicleTransform.forward * -forwardspeed * WaterForwardDrag * depth, FloatPoints[currentfloatpoint].position, ForceMode.Force);

        currentfloatpoint++;
        if (currentfloatpoint == FloatPoints.Length) { currentfloatpoint = 0; }
    }
}
