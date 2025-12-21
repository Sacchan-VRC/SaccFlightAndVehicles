//MODIFIED BY SACC
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(10010)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KitKatsHMCSController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Text HUDText_G;
        public Text HUDText_mach;
        public Text HUDText_altitude;
        [Tooltip("Meters * (default=feet)")]
        public float AltitudeConversion = 3.28084f;
        public Text HUDText_knots;
        [Tooltip("Meters * (default=knots)")]
        public float SpeedConversion = 1.9438445f;
        public Text HUDText_knotsairspeed;
        public Text HUDText_angleofattack;
        public Text[] CopyTextFrom;
        public Text[] CopyTextTo;
        public GameObject[] CopyActiveObjectsFrom;
        public GameObject[] CopyActiveObjectsTo;
        [Tooltip("Hud element that shows yaw angle")]
        public Transform HeadingIndicator;
        [Range(-180, 180), SerializeField] float HeadingOffset = 0;
        public Transform Healthbar;
        private SaccEntity EntityControl;
        [Tooltip("Local distance projected forward for objects that move dynamically, only adjust if the hud is moved forward in order to make it appear smaller")]
        public float distance_from_head = 1.333f;
        private float maxGs = 0f;
        private float check = 0;
        private int showvel;
        private Transform VehicleTransform;
        private float SeaLevel;
        private Transform CenterOfMass;
        VRCPlayerApi localPlayer;
        [Header("Dynamic HMCS?")]
        [Tooltip("Turn this off if you want the hmcs to stay on no matter the angle.")]
        public bool DynamicHMCS = true;
        [Tooltip("This is the angle off the nose of the plane that the hmcs will be disabled at.")]
        public float HMCSBottomAngle = 55;
        public float HUDx = 107;
        public float HUDy = 25;
        public float Dashx = 68;
        public float Dashy = 31;
        GameObject Child;
        private float FullHealth;
        public bool DebugMode;
        float HMCSx;
        float HMCSy;
        private void Start()
        {
            Child = gameObject.transform.GetChild(0).gameObject;
            if (!DynamicHMCS) { Child.SetActive(true); } else { Child.SetActive(false); }

            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleTransform = EntityControl.transform;

            SeaLevel = (float)SAVControl.GetProgramVariable("SeaLevel");
            CenterOfMass = EntityControl.CenterOfMass;

            localPlayer = Networking.LocalPlayer;
        }
        private void OnEnable()
        {
            maxGs = 0f;
            FullHealth = (float)SAVControl.GetProgramVariable("FullHealth");
        }
        private void LateUpdate()
        {
            if (DynamicHMCS)
            {
                HMCSx = Vector3.Angle(transform.forward, -EntityControl.transform.up);
                HMCSy = Mathf.Abs(Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, EntityControl.transform.up), EntityControl.transform.forward, EntityControl.transform.up));
                if (HMCSx < HMCSBottomAngle || HMCSy < HUDy && HMCSx < HUDx || HMCSy < Dashy && HMCSx < Dashx)
                {
                    Child.SetActive(false);
                }
                else
                {
                    Child.SetActive(true);
                }
            }
            // if (DebugMode)
            // {
            //     Debug.Log("HMCS rotation x: " + HMCSx);
            //     Debug.Log("HMCS rotation y: " + HMCSy);
            // }

            transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

            Vector3 PlayerHeadLook = transform.rotation.eulerAngles;

            float SmoothDeltaTime = Time.smoothDeltaTime;

            //Heading indicator
            float angleCompass = PlayerHeadLook.y + HeadingOffset;
            if (angleCompass > 360) angleCompass -= 360;
            if (angleCompass < 0) angleCompass += 360;
            angleCompass -= 180;
            if (HeadingIndicator) { HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -angleCompass, 0)); }
            /////////////////

            //Health
            float Health = (float)SAVControl.GetProgramVariable("Health");
            float PercentageHP = Mathf.Max(Health, 0) / FullHealth;
            if (Healthbar) Healthbar.localScale = new Vector3(PercentageHP, 1, 1);
            /////////////////

            //updating numbers 10 times a second
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
                if (HUDText_knots) { HUDText_knots.text = (((float)SAVControl.GetProgramVariable("Speed")) * SpeedConversion).ToString("F0"); }
                if (HUDText_knotsairspeed) { HUDText_knotsairspeed.text = (((float)SAVControl.GetProgramVariable("AirSpeed")) * SpeedConversion).ToString("F0"); }

                if (HUDText_angleofattack)
                {
                    if ((float)SAVControl.GetProgramVariable("Speed") < 2)
                    { HUDText_angleofattack.text = System.String.Empty; }
                    else
                    { HUDText_angleofattack.text = ((float)SAVControl.GetProgramVariable("AngleOfAttack")).ToString("F0"); }
                }
                for (int i = 0; i < CopyTextFrom.Length; i++)
                {
                    CopyTextTo[i].text = CopyTextFrom[i].text;
                }
                for (int i = 0; i < CopyActiveObjectsFrom.Length; i++)
                {
                    if (CopyActiveObjectsTo[i].activeSelf != CopyActiveObjectsFrom[i].activeSelf)
                        CopyActiveObjectsTo[i].SetActive(CopyActiveObjectsFrom[i].activeSelf);
                }
                check = 0;
            }
            check += SmoothDeltaTime;
        }
    }
}