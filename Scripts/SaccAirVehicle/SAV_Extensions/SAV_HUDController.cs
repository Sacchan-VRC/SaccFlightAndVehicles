
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_HUDController : UdonSharpBehaviour
    {
        [Tooltip("Transform of the pilot seat's target eye position, HUDController is automatically moved to this position in Start() to ensure perfect alignment. Not required")]
        public Transform PilotSeatAdjusterTarget;
        public UdonSharpBehaviour SAVControl;
        public Animator HUDAnimator;
        public Text HUDText_G;
        public Text HUDText_mach;
        public Text HUDText_altitude;
        public Text HUDText_radaraltitude;
        [Tooltip("Subtract this many meters from radar altitude to match 0 to the bottom of the vehicle")]
        public float RadarAltitudeOffset = 1.5f;
        [Tooltip("Meters * (default=feet)")]
        public float AltitudeConversion = 3.28084f;
        public Text HUDText_knots;
        [Tooltip("Meters * (default=knots)")]
        public float SpeedConversion = 1.9438445f;
        public Text HUDText_knotsairspeed;
        public Text HUDText_angleofattack;
        [Tooltip("Hud element that points toward the gruond")]
        public Transform DownIndicator;
        [Tooltip("Hud element that shows pitch angle")]
        public Transform ElevationIndicator;
        [Tooltip("Hud element that shows yaw angle")]
        public Transform HeadingIndicator;
        [Tooltip("Hud element that shows vehicle's direction of movement")]
        public Transform VelocityIndicator;
        private SaccEntity EntityControl;
        [Tooltip("Local distance projected forward for objects that move dynamically, only adjust if the hud is moved forward in order to make it appear smaller")]
        public float distance_from_head = 1.333f;
        private float maxGs = 0f;
        private Vector3 startingpos;
        private float check = 0;
        private int showvel;
        private Vector3 TargetDir = Vector3.zero;
        private Vector3 TargetSpeed;
        private float FullGunAmmoDivider;
        private Transform VehicleTransform;
        private SAV_EffectsController EffectsControl;
        private float SeaLevel;
        private Transform CenterOfMass;
        VRCPlayerApi localPlayer;
        private Vector3 Vel_Lerper;
        private float Vel_UpdateInterval;
        private float Vel_UpdateTime;
        private Vector3 Vel_PredictedCurVel;
        private Vector3 Vel_LastCurVel;
        private Vector3 Vel_NormalizedExtrapDir;
        private void Start()
        {

            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            HUDAnimator = EntityControl.GetComponent<Animator>();
            VehicleTransform = EntityControl.transform;

            if (PilotSeatAdjusterTarget) { transform.position = PilotSeatAdjusterTarget.position; }

            SeaLevel = (float)SAVControl.GetProgramVariable("SeaLevel");
            CenterOfMass = EntityControl.CenterOfMass;

            localPlayer = Networking.LocalPlayer;
        }
        private void OnEnable()
        {
            maxGs = 0f;
        }
        private void LateUpdate()
        {
            float SmoothDeltaTime = Time.smoothDeltaTime;

            //Velocity indicator
            if (VelocityIndicator)
            {
                Vector3 currentvel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                if (currentvel.magnitude < 2)
                { currentvel = -Vector3.up * 2; }//straight down instead of spazzing out when moving very slow
                if ((bool)SAVControl.GetProgramVariable("IsOwner"))
                {
                    VelocityIndicator.position = transform.position + currentvel;
                }
                else
                {
                    //extrapolate CurrentVel and lerp towards it to smooth out the velocity indicator of non-owners
                    if (currentvel != Vel_LastCurVel)
                    {
                        float tim = Time.time;
                        Vel_UpdateInterval = tim - Vel_UpdateTime;
                        Vel_NormalizedExtrapDir = (currentvel - Vel_LastCurVel) * (1 / Vel_UpdateInterval);
                        Vel_LastCurVel = currentvel;
                        Vel_UpdateTime = tim;
                    }
                    Vel_PredictedCurVel = currentvel + (Vel_NormalizedExtrapDir * (Time.time - Vel_UpdateTime));
                    Vel_Lerper = Vector3.Lerp(Vel_Lerper, Vel_PredictedCurVel, 9f * Time.smoothDeltaTime);
                    VelocityIndicator.position = transform.position + Vel_Lerper;
                }
                VelocityIndicator.localPosition = VelocityIndicator.localPosition.normalized * distance_from_head;
                VelocityIndicator.rotation = Quaternion.LookRotation(VelocityIndicator.position - gameObject.transform.position, gameObject.transform.up);//This makes it face the pilot.
            }
            /////////////////


            //Heading indicator
            Vector3 VehicleEuler = EntityControl.transform.rotation.eulerAngles;
            if (HeadingIndicator) { HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -VehicleEuler.y, 0)); }
            /////////////////

            //Elevation indicator
            if (ElevationIndicator) { ElevationIndicator.rotation = Quaternion.Euler(new Vector3(0, VehicleEuler.y, 0)); }
            /////////////////

            //Down indicator
            if (DownIndicator) { DownIndicator.localRotation = Quaternion.Euler(new Vector3(0, 0, -VehicleEuler.z)); }
            /////////////////

            //updating numbers 3~ times a second
            if (check > .3)//update text
            {
                if (HUDText_G)
                {
                    if (Mathf.Abs(maxGs) < Mathf.Abs((float)SAVControl.GetProgramVariable("VertGs")))
                    { maxGs = (float)SAVControl.GetProgramVariable("VertGs"); }
                    HUDText_G.text = string.Concat(((float)SAVControl.GetProgramVariable("VertGs")).ToString("F1"), "\n", maxGs.ToString("F1"));
                }
                if (HUDText_mach) { HUDText_mach.text = (((float)SAVControl.GetProgramVariable("Speed")) / 343f).ToString("F2"); }
                if (HUDText_altitude)
                {
                    HUDText_altitude.text = string.Concat((((Vector3)SAVControl.GetProgramVariable("CurrentVel")).y * 60 * AltitudeConversion).ToString("F0"),
                    "\n", ((CenterOfMass.position.y - SeaLevel) * AltitudeConversion).ToString("F0"));
                }
                if (HUDText_radaraltitude)
                {
                    RaycastHit alt;
                    if (Physics.Raycast(CenterOfMass.position, Vector3.down, out alt, Mathf.Infinity, 2065 /* Default, Water and Environment */, QueryTriggerInteraction.Collide))
                    { HUDText_radaraltitude.text = ((alt.distance - RadarAltitudeOffset) * AltitudeConversion).ToString("F0"); }
                    else
                    { HUDText_radaraltitude.text = string.Empty; }
                }
                if (HUDText_knots) { HUDText_knots.text = (((float)SAVControl.GetProgramVariable("Speed")) * SpeedConversion).ToString("F0"); }
                if (HUDText_knotsairspeed) { HUDText_knotsairspeed.text = (((float)SAVControl.GetProgramVariable("AirSpeed")) * SpeedConversion).ToString("F0"); }

                if (HUDText_angleofattack)
                {
                    if ((float)SAVControl.GetProgramVariable("Speed") < 2)
                    { HUDText_angleofattack.text = System.String.Empty; }
                    else
                    { HUDText_angleofattack.text = ((float)SAVControl.GetProgramVariable("AngleOfAttack")).ToString("F0"); }
                }
                check = 0;
            }
            check += SmoothDeltaTime;
        }
    }
}
