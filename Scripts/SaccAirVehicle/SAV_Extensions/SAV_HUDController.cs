
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class SAV_HUDController : UdonSharpBehaviour
{
    [SerializeField] private Transform PilotSeatAdjusterTarget;
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private Animator HUDAnimator;
    [SerializeField] private Text HUDText_G;
    [SerializeField] private Text HUDText_mach;
    [SerializeField] private Text HUDText_altitude;
    [SerializeField] private Text HUDText_knots;
    [SerializeField] private Text HUDText_knotsairspeed;
    [SerializeField] private Text HUDText_angleofattack;
    [SerializeField] private Transform DownIndicator;
    [SerializeField] private Transform ElevationIndicator;
    [SerializeField] private Transform HeadingIndicator;
    [SerializeField] private Transform VelocityIndicator;
    [SerializeField] private float BulletSpeed = 1050;
    [SerializeField] private Transform PitchRoll;
    [SerializeField] private Transform Yaw;
    [SerializeField] private Transform LStickDisplayHighlighter;
    [SerializeField] private Transform RStickDisplayHighlighter;
    [SerializeField] private AudioSource SwitchFunctionSound;
    private SaccEntity EntityControl;
    private bool SwitchFunctionSoundNULL;
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
    private SAV_EffectsController EffectsControl;
    private float LStickFuncDegrees;
    private float RStickFuncDegrees;
    private int FUEL_STRING = Animator.StringToHash("fuel");
    private int GUNAMMO_STRING = Animator.StringToHash("gunammo");
    VRCPlayerApi localPlayer;
    private int LStickSelectionLastFrame = -1;
    private int RStickSelectionLastFrame = -1;
    private void Start()
    {
        SwitchFunctionSoundNULL = SwitchFunctionSound == null;

        HUDAnimator = SAVControl.EntityControl.GetComponent<Animator>();
        EntityControl = SAVControl.EntityControl;
        InputsZeroPos = PitchRoll.localPosition;
        VehicleTransform = SAVControl.EntityControl.transform;

        float fuel = SAVControl.Fuel;
        FullFuelDivider = 1f / (fuel > 0 ? fuel : 10000000);

        if (PilotSeatAdjusterTarget != null) { transform.position = PilotSeatAdjusterTarget.position; }

        RStickFuncDegrees = EntityControl.RStickFuncDegrees;
        LStickFuncDegrees = EntityControl.LStickFuncDegrees;

        localPlayer = Networking.LocalPlayer;
    }
    private void OnEnable()
    {
        maxGs = 0f;
        LStickSelectionLastFrame = -1;
        RStickSelectionLastFrame = -1;
        LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180);
        RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180);
    }
    private void LateUpdate()
    {
        float SmoothDeltaTime = Time.smoothDeltaTime;

        //Velocity indicator
        Vector3 tempvel;
        if (SAVControl.CurrentVel.magnitude < 2)
        {
            tempvel = -Vector3.up * 2;//straight down instead of spazzing out when moving very slow
        }
        else
        {
            tempvel = SAVControl.CurrentVel;
        }

        VelocityIndicator.position = transform.position + tempvel;
        VelocityIndicator.localPosition = VelocityIndicator.localPosition.normalized * distance_from_head;
        /////////////////


        //Heading indicator
        Vector3 VehicleEuler = SAVControl.EntityControl.transform.rotation.eulerAngles;
        HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -VehicleEuler.y, 0));
        /////////////////

        //Elevation indicator
        ElevationIndicator.rotation = Quaternion.Euler(new Vector3(0, VehicleEuler.y, 0));
        /////////////////

        //Down indicator
        DownIndicator.localRotation = Quaternion.Euler(new Vector3(0, 0, -VehicleEuler.z));
        /////////////////

        int StickSelection = EntityControl.LStickSelection;
        if (StickSelection != LStickSelectionLastFrame)
        {
            if (!SwitchFunctionSoundNULL) { SwitchFunctionSound.Play(); }
            LStickSelectionLastFrame = StickSelection;
            if (StickSelection < 0)
            { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); }
            else
            {
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, LStickFuncDegrees * StickSelection, 0);
            }
        }
        LStickSelectionLastFrame = StickSelection;
        StickSelection = EntityControl.RStickSelection;
        if (StickSelection != RStickSelectionLastFrame)
        {
            if (!SwitchFunctionSoundNULL) { SwitchFunctionSound.Play(); }
            RStickSelectionLastFrame = StickSelection;
            if (StickSelection < 0)
            { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180); }
            else
            {
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, RStickFuncDegrees * StickSelection, 0);
            }
        }


        //updating numbers 3~ times a second
        if (check > .3)//update text
        {
            if (Mathf.Abs(maxGs) < Mathf.Abs(SAVControl.VertGs))
            { maxGs = SAVControl.VertGs; }
            HUDText_G.text = string.Concat(SAVControl.VertGs.ToString("F1"), "\n", maxGs.ToString("F1"));
            HUDText_mach.text = ((SAVControl.Speed) / 343f).ToString("F2");
            HUDText_altitude.text = string.Concat((SAVControl.CurrentVel.y * 60 * 3.28084f).ToString("F0"), "\n", ((SAVControl.CenterOfMass.position.y + -SAVControl.SeaLevel) * 3.28084f).ToString("F0"));
            HUDText_knots.text = ((SAVControl.Speed) * 1.9438445f).ToString("F0");
            HUDText_knotsairspeed.text = ((SAVControl.AirSpeed) * 1.9438445f).ToString("F0");

            if (SAVControl.Speed < 2)
            {
                HUDText_angleofattack.text = System.String.Empty;
            }
            else
            {
                HUDText_angleofattack.text = SAVControl.AngleOfAttack.ToString("F0");
            }
            check = 0;
        }
        check += SmoothDeltaTime;

        HUDAnimator.SetFloat(FUEL_STRING, SAVControl.Fuel * FullFuelDivider);
    }
}