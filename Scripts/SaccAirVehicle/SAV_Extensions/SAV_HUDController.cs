
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class SAV_HUDController : UdonSharpBehaviour
{
    [Tooltip("Transform of the pilot seat's target eye position, HUDContrller is automatically moved to this position in Start() to ensure perfect alignment")]
    [SerializeField] private Transform PilotSeatAdjusterTarget;
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private Animator HUDAnimator;
    [SerializeField] private Text HUDText_G;
    [SerializeField] private Text HUDText_mach;
    [SerializeField] private Text HUDText_altitude;
    [SerializeField] private Text HUDText_knots;
    [SerializeField] private Text HUDText_knotsairspeed;
    [SerializeField] private Text HUDText_angleofattack;
    [Tooltip("Hud element that points toward the gruond")]
    [SerializeField] private Transform DownIndicator;
    [Tooltip("Hud element that shows pitch angle")]
    [SerializeField] private Transform ElevationIndicator;
    [Tooltip("Hud element that shows yaw angle")]
    [SerializeField] private Transform HeadingIndicator;
    [Tooltip("Hud element that shows vehicle's direction of movement")]
    [SerializeField] private Transform VelocityIndicator;
    private SaccEntity EntityControl;
    [Tooltip("Local distance projected forward for objects that move dynamically, only adjust if the hud is moved forward in order to make it appear smaller")]
    public float distance_from_head = 1.333f;
    private float maxGs = 0f;
    private Vector3 startingpos;
    private float check = 0;
    private int showvel;
    private Vector3 TargetDir = Vector3.zero;
    private Vector3 TargetSpeed;
    private float FullFuelDivider;
    private float FullGunAmmoDivider;
    private Transform VehicleTransform;
    private SAV_EffectsController EffectsControl;
    private float LStickFuncDegrees;
    private float RStickFuncDegrees;
    private int FUEL_STRING = Animator.StringToHash("fuel");
    private int GUNAMMO_STRING = Animator.StringToHash("gunammo");
    VRCPlayerApi localPlayer;
    private void Start()
    {

        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        HUDAnimator = EntityControl.GetComponent<Animator>();
        VehicleTransform = EntityControl.transform;

        float fuel = (float)SAVControl.GetProgramVariable("Fuel");
        FullFuelDivider = 1f / (fuel > 0 ? fuel : 10000000);

        if (PilotSeatAdjusterTarget != null) { transform.position = PilotSeatAdjusterTarget.position; }

        RStickFuncDegrees = EntityControl.RStickFuncDegrees;
        LStickFuncDegrees = EntityControl.LStickFuncDegrees;

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
        Vector3 tempvel;
        if (((Vector3)SAVControl.GetProgramVariable("CurrentVel")).magnitude < 2)
        {
            tempvel = -Vector3.up * 2;//straight down instead of spazzing out when moving very slow
        }
        else
        {
            tempvel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
        }

        VelocityIndicator.position = transform.position + tempvel;
        VelocityIndicator.localPosition = VelocityIndicator.localPosition.normalized * distance_from_head;
        /////////////////


        //Heading indicator
        Vector3 VehicleEuler = EntityControl.transform.rotation.eulerAngles;
        HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -VehicleEuler.y, 0));
        /////////////////

        //Elevation indicator
        ElevationIndicator.rotation = Quaternion.Euler(new Vector3(0, VehicleEuler.y, 0));
        /////////////////

        //Down indicator
        DownIndicator.localRotation = Quaternion.Euler(new Vector3(0, 0, -VehicleEuler.z));
        /////////////////

        //updating numbers 3~ times a second
        if (check > .3)//update text
        {
            if (Mathf.Abs(maxGs) < Mathf.Abs((float)SAVControl.GetProgramVariable("VertGs")))
            { maxGs = (float)SAVControl.GetProgramVariable("VertGs"); }
            HUDText_G.text = string.Concat(((float)SAVControl.GetProgramVariable("VertGs")).ToString("F1"), "\n", maxGs.ToString("F1"));
            HUDText_mach.text = (((float)SAVControl.GetProgramVariable("Speed")) / 343f).ToString("F2");
            HUDText_altitude.text = string.Concat((((Vector3)SAVControl.GetProgramVariable("CurrentVel")).y * 60 * 3.28084f).ToString("F0"), "\n", ((((Transform)SAVControl.GetProgramVariable("CenterOfMass")).position.y + -(float)SAVControl.GetProgramVariable("SeaLevel")) * 3.28084f).ToString("F0"));
            HUDText_knots.text = (((float)SAVControl.GetProgramVariable("Speed")) * 1.9438445f).ToString("F0");
            HUDText_knotsairspeed.text = (((float)SAVControl.GetProgramVariable("AirSpeed")) * 1.9438445f).ToString("F0");

            if ((float)SAVControl.GetProgramVariable("Speed") < 2)
            {
                HUDText_angleofattack.text = System.String.Empty;
            }
            else
            {
                HUDText_angleofattack.text = ((float)SAVControl.GetProgramVariable("AngleOfAttack")).ToString("F0");
            }
            check = 0;
        }
        check += SmoothDeltaTime;

        HUDAnimator.SetFloat(FUEL_STRING, (float)SAVControl.GetProgramVariable("Fuel") * FullFuelDivider);
    }
}