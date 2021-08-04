
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class HUDController : UdonSharpBehaviour
{
    [SerializeField] private Transform PilotSeatAdjusterTarget;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private Animator HUDAnimator;
    [SerializeField] private Text HUDText_G;
    [SerializeField] private Text HUDText_mach;
    [SerializeField] private Text HUDText_altitude;
    [SerializeField] private Text HUDText_knotstarget;
    [SerializeField] private Text HUDText_knots;
    [SerializeField] private Text HUDText_knotsairspeed;
    [SerializeField] private Text HUDText_angleofattack;
    [SerializeField] private GameObject HudCrosshairGun;
    [SerializeField] private GameObject HudCrosshair;
    [SerializeField] private GameObject HudHold;
    [SerializeField] private GameObject HudLimit;
    [SerializeField] private GameObject HudAB;
    [SerializeField] private Transform DownIndicator;
    [SerializeField] private Transform ElevationIndicator;
    [SerializeField] private Transform HeadingIndicator;
    [SerializeField] private Transform VelocityIndicator;
    [SerializeField] private float BulletSpeed = 1050;
    [SerializeField] private Transform PitchRoll;
    [SerializeField] private Transform Yaw;
    [SerializeField] private Transform LStickDisplayHighlighter;
    [SerializeField] private Transform RStickDisplayHighlighter;
    public float distance_from_head = 1.333f;
    private float maxGs = 0f;
    private Vector3 InputsZeroPos;
    private Vector3 startingpos;
    private float check = 0;
    private int showvel;
    private Vector3 TargetDir = Vector3.zero;
    private Vector3 TargetSpeed;
    private float FullFuelDivider;
    private float FullGunAmmoDivider;
    private Transform VehicleTransform;
    private EffectsController EffectsControl;
    private float LStickFuncDegrees;
    private float RStickFuncDegrees;
    private int FUEL_STRING = Animator.StringToHash("fuel");
    private int GUNAMMO_STRING = Animator.StringToHash("gunammo");
    VRCPlayerApi localPlayer;
    private int LStickSelectionLastFrame = -1;
    private int RStickSelectionLastFrame = -1;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(HUDText_G != null, "Start: HUDText_G != null");
        Assert(HUDText_mach != null, "Start: HUDText_mach != null");
        Assert(HUDText_altitude != null, "Start: HUDText_altitude != null");
        Assert(HUDText_knotstarget != null, "Start: HUDText_knotstarget != null");
        Assert(HUDText_knots != null, "Start: HUDText_knots != null");
        Assert(HUDText_knotsairspeed != null, "Start: HUDText_knotsairspeed != null");
        Assert(HUDText_angleofattack != null, "Start: HUDText_angleofattack != null");
        Assert(HudCrosshairGun != null, "Start: HudCrosshairGun != null");
        Assert(HudCrosshair != null, "Start: HudCrosshair != null");
        Assert(HudHold != null, "Start: HudHold != null");
        Assert(HudLimit != null, "Start: HudLimit != null");
        Assert(HudAB != null, "Start: HudAB != null");
        Assert(DownIndicator != null, "Start: DownIndicator != null");
        Assert(ElevationIndicator != null, "Start: ElevationIndicator != null");
        Assert(HeadingIndicator != null, "Start: HeadingIndicator != null");
        Assert(VelocityIndicator != null, "Start: VelocityIndicator != null");
        Assert(LStickDisplayHighlighter != null, "Start: LStickDisplayHighlighter != null");
        Assert(RStickDisplayHighlighter != null, "Start: RStickDisplayHighlighter != null");
        Assert(PitchRoll != null, "Start: PitchRoll != null");
        Assert(Yaw != null, "Start: Yaw != null");

        HUDAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        InputsZeroPos = PitchRoll.localPosition;
        VehicleTransform = EngineControl.VehicleMainObj.transform;

        float fuel = EngineControl.Fuel;
        FullFuelDivider = 1f / (fuel > 0 ? fuel : 10000000);

        if (PilotSeatAdjusterTarget != null) { transform.position = PilotSeatAdjusterTarget.position; }

        RStickFuncDegrees = EngineControl.RStickFuncDegrees;
        LStickFuncDegrees = EngineControl.LStickFuncDegrees;

        localPlayer = Networking.LocalPlayer;
    }
    private void OnEnable()
    {
        maxGs = 0f;
        LStickSelectionLastFrame = -1;
        RStickSelectionLastFrame = -1;
    }
    private void LateUpdate()
    {
        float SmoothDeltaTime = Time.smoothDeltaTime;

        //Velocity indicator
        Vector3 tempvel;
        if (EngineControl.CurrentVel.magnitude < 2)
        {
            tempvel = -Vector3.up * 2;//straight down instead of spazzing out when moving very slow
        }
        else
        {
            tempvel = EngineControl.CurrentVel;
        }

        VelocityIndicator.position = transform.position + tempvel;
        VelocityIndicator.localPosition = VelocityIndicator.localPosition.normalized * distance_from_head;
        /////////////////


        //Heading indicator
        Vector3 VehicleEuler = EngineControl.VehicleMainObj.transform.rotation.eulerAngles;
        HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -VehicleEuler.y, 0));
        /////////////////

        //Elevation indicator
        ElevationIndicator.rotation = Quaternion.Euler(new Vector3(0, VehicleEuler.y, 0));
        /////////////////

        //Down indicator
        DownIndicator.localRotation = Quaternion.Euler(new Vector3(0, 0, -VehicleEuler.z));
        /////////////////

        //LIMITS indicator
        if (EngineControl.FlightLimitsEnabled)
        {
            HudLimit.SetActive(true);
        }
        else { HudLimit.SetActive(false); }

        //Alt. HOLD indicator
        if (EngineControl.AltHold)
        {
            HudHold.SetActive(true);
        }
        else { HudHold.SetActive(false); }

        int StickSelection = EngineControl.LStickSelection;
        if (StickSelection != LStickSelectionLastFrame)
        {
            LStickSelectionLastFrame = StickSelection;
            if (StickSelection < 0)
            { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); }
            else
            {
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, LStickFuncDegrees * StickSelection, 0);
            }
        }
        LStickSelectionLastFrame = StickSelection;

        StickSelection = EngineControl.RStickSelection;
        if (StickSelection != RStickSelectionLastFrame)
        {
            RStickSelectionLastFrame = StickSelection;
            if (StickSelection < 0)
            { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); }
            else
            {
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, RStickFuncDegrees * StickSelection, 0);
            }
        }


        //Cruise Control target knots
        if (EngineControl.Cruise)
        {
            HUDText_knotstarget.text = ((EngineControl.SetSpeed) * 1.9438445f).ToString("F0");
        }
        else { HUDText_knotstarget.text = string.Empty; }



        //updating numbers 3~ times a second
        if (check > .3)//update text
        {
            if (EngineControl.Gs > maxGs) { maxGs = EngineControl.Gs; }
            HUDText_G.text = string.Concat(EngineControl.Gs.ToString("F1"), "\n", maxGs.ToString("F1"));
            HUDText_mach.text = ((EngineControl.Speed) / 343f).ToString("F2");
            HUDText_altitude.text = string.Concat((EngineControl.CurrentVel.y * 60 * 3.28084f).ToString("F0"), "\n", ((EngineControl.CenterOfMass.position.y + -EngineControl.SeaLevel) * 3.28084f).ToString("F0"));
            HUDText_knots.text = ((EngineControl.Speed) * 1.9438445f).ToString("F0");
            HUDText_knotsairspeed.text = ((EngineControl.AirSpeed) * 1.9438445f).ToString("F0");

            if (EngineControl.Speed < 2)
            {
                HUDText_angleofattack.text = System.String.Empty;
            }
            else
            {
                HUDText_angleofattack.text = EngineControl.AngleOfAttack.ToString("F0");
            }
            check = 0;
        }
        check += SmoothDeltaTime;

        HUDAnimator.SetFloat(FUEL_STRING, EngineControl.Fuel * FullFuelDivider);
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}