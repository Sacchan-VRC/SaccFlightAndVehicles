
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccGaugeController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Animator[] DialsAnimator;
        private Transform VehicleTransform;
        private Transform CenterOfMass;
        private bool HasHorizon;
        void Start()
        {
            CenterOfMass = (Transform)SAVControl.GetProgramVariable("CenterOfMass");
            VehicleTransform = (Transform)SAVControl.GetProgramVariable("VehicleTransform");
            FullFuel = (float)SAVControl.GetProgramVariable("FullFuel");
            HasHorizon = Horizon.Length > 0;
            SeaLevel = (float)SAVControl.GetProgramVariable("SeaLevel");
        }
        private void LateUpdate()
        {
            DoAltitude();
            DoFuel();
            DoSpeed();
            DoEngine();
            DoSlip();
            DoHorizon();
            DoAscent();
            DoCompass();
        }
        [Header("Altitude")]
        public string AltitudeFloatName100 = "altitude100";
        public string AltitudeFloatName1000 = "altitude1000";
        public string AltitudeFloatName10000 = "altitude10000";
        private float SeaLevel;
        public float AltitudeConversion = 3.28084f;
        public void DoAltitude()
        {
            float alt = (CenterOfMass.position.y - SeaLevel) * AltitudeConversion;
            float alt100s = alt / 100f;
            float alt1000s = alt / 1000f;
            float alt10000s = alt / 10000f;
            for (int i = 0; i < DialsAnimator.Length; i++)
            {
                DialsAnimator[i].SetFloat(AltitudeFloatName100, alt100s);
                DialsAnimator[i].SetFloat(AltitudeFloatName1000, alt1000s);
                DialsAnimator[i].SetFloat(AltitudeFloatName10000, alt10000s);
            }
        }
        [Header("Fuel")]
        float FullFuel;
        public string FuelFloatName = "fuel";
        public void DoFuel()
        {
            float fuel = (float)SAVControl.GetProgramVariable("Fuel") / FullFuel;
            for (int i = 0; i < DialsAnimator.Length; i++)
            { DialsAnimator[i].SetFloat(FuelFloatName, fuel); }
        }
        [Header("Speed")]
        public float MaxSpeed = 257.222221222f;//500 / knotsconversion;
        public string SpeedFloatName = "speed";
        public void DoSpeed()
        {
            float speed = (float)SAVControl.GetProgramVariable("Speed") / MaxSpeed;
            for (int i = 0; i < DialsAnimator.Length; i++)
            { DialsAnimator[i].SetFloat(SpeedFloatName, speed); }
        }
        [Header("Engine")]
        public string EngineFloatName = "engine";
        public void DoEngine()
        {
            float engine = (float)SAVControl.GetProgramVariable("EngineOutput");
            for (int i = 0; i < DialsAnimator.Length; i++)
            { DialsAnimator[i].SetFloat(EngineFloatName, engine); }
        }
        [Header("Slip")]
        public string SlipFloatName = "slip";
        public float MaxSlip = 15f;
        public void DoSlip()
        {
            if ((float)SAVControl.GetProgramVariable("Speed") < 5)
            {
                for (int i = 0; i < DialsAnimator.Length; i++)
                {
                    DialsAnimator[i].SetFloat(SlipFloatName, 0f);
                }
                return;
            }
            Vector3 VelFlattened = Vector3.ProjectOnPlane((Vector3)SAVControl.GetProgramVariable("CurrentVel"), VehicleTransform.up);
            Vector3 forward = VehicleTransform.forward;
            float slip = Vector3.SignedAngle(forward, VelFlattened, VehicleTransform.up) / MaxSlip;
            // float slip = Vector3.Dot(VehicleTransform.right, (Vector3)SAVControl.GetProgramVariable("CurrentVel")) / MaxSlip;
            slip = Mathf.Clamp((slip * .5f) + .5f, 0, 1);
            for (int i = 0; i < DialsAnimator.Length; i++)
            {
                DialsAnimator[i].SetFloat(SlipFloatName, slip);
            }
        }
        [Header("Horizon")]
        public Transform[] Horizon;
        public Vector3 HorizonRotOffset;
        // public bool xSwap;
        // public bool ySwap;
        // public bool zSwap;
        // public bool wSwap;
        // public string PitchFloatName = "pitch";
        // public string RollFloatName = "roll";
        public void DoHorizon()
        {
            //don't think this can work due to gimble lock
            // float pitch = (VehicleTransform.eulerAngles.x) / 180f;
            // pitch = pitch - (int)pitch;
            // float roll = (VehicleTransform.eulerAngles.z) / 360f;
            // roll = roll - (int)roll;

            // for (int i = 0; i < DialsAnimator.Length; i++)
            // {
            //     DialsAnimator[i].SetFloat(PitchFloatName, pitch);
            //     DialsAnimator[i].SetFloat(RollFloatName, roll);
            // }
            if (!HasHorizon) { return; }
            Quaternion newrot = Quaternion.Euler(HorizonRotOffset.x, HorizonRotOffset.y, HorizonRotOffset.z);
            Quaternion planerot = Quaternion.AngleAxis(VehicleTransform.eulerAngles.y, Vector3.up);
            newrot = planerot * newrot;
            Horizon[0].rotation = newrot;
            Quaternion locrot = Horizon[0].localRotation;
            // if (xSwap)
            // { locrot.x = -locrot.x; }
            // if (ySwap)
            locrot.y = -locrot.y;
            // if (zSwap)
            // { locrot.z = -locrot.z; }
            // if (wSwap)
            locrot.w = -locrot.w;
            for (int i = 0; i < Horizon.Length; i++)
            {
                Horizon[i].localRotation = locrot;
            }
            //can probably be done more efficiently something like this
            // Horizon.localRotation = Quaternion.Euler(-VehicleTransform.eulerAngles.x + HorizonRotOffset.x, HorizonRotOffset.y, -VehicleTransform.eulerAngles.z + HorizonRotOffset.z);
        }
        [Header("Ascent")]
        public string AscentFloatName = "ascent";
        public float MaxAscent = 5000;
        public float AscentConversion = 3.28084f;
        public void DoAscent()
        {
            float ascent = (Vector3.Dot(Vector3.up, (Vector3)SAVControl.GetProgramVariable("CurrentVel")) * AscentConversion * 60f) / MaxAscent;
            ascent = Mathf.Clamp((ascent * .5f) + .5f, 0, 1);
            for (int i = 0; i < DialsAnimator.Length; i++)
            {
                DialsAnimator[i].SetFloat(AscentFloatName, ascent);
            }
        }
        [Header("Compass")]
        [Header("Axis to rotate on x=1,2=y,3=z, negative to invert")]
        public byte CompassAxis = 2;
        public float CompassOffset;
        public string CompassFloatName = "compass";
        public void DoCompass()
        {
            float compass = (VehicleTransform.eulerAngles.y + CompassOffset) / 360f;
            compass = compass - (int)compass;
            for (int i = 0; i < DialsAnimator.Length; i++)
            {
                DialsAnimator[i].SetFloat(CompassFloatName, compass);
            }
        }
    }
}