
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class HUDController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public Material SmokeColorIndicator;
    public Text HUDText_G;
    public Text HUDText_mach;
    public Text HUDText_altitude;
    public Text HUDText_knotstarget;
    public Text HUDText_knots;
    public Text HUDText_knotsairspeed;
    public Text HUDText_angleofattack;
    public Text HUDText_AAM_ammo;
    public Text HUDText_AGM_ammo;
    public GameObject HudCrosshairGun;
    public GameObject HudCrosshair;
    public GameObject HudLimit;
    public GameObject HudAB;
    public Transform DownIndicator;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    public Transform VelocityIndicator;
    public Transform AAMTargetIndicator;
    public Transform LStickDisplayHighlighter;
    public Transform RStickDisplayHighlighter;
    public Transform PitchRoll;
    public Transform Yaw;
    public Transform TrimPitch;
    public Transform TrimYaw;
    public GameObject AGMScreen;
    public GameObject LStick_funcon1;
    public GameObject LStick_funcon2;
    public GameObject LStick_funcon3;
    public GameObject LStick_funcon4;
    public GameObject LStick_funcon6;
    public GameObject LStick_funcon7;
    public GameObject LStick_funcon8;
    public GameObject RStick_funcon3;
    public GameObject RStick_funcon4;
    public GameObject RStick_funcon5;
    public GameObject RStick_funcon6;
    public GameObject RStick_funcon7;
    private Animator PlaneAnimator;
    private const float distance_from_head = 1.333f;
    private float maxGs = 0f;
    private Vector3 InputsZeroPos;
    private Vector3 tempvel = Vector3.zero;
    private Vector3 startingpos;
    private float check = 0;
    [System.NonSerializedAttribute] [HideInInspector] public float MenuSoundCheckLast = 0;
    private Vector3 temprot;
    private int showvel;
    const float InputSquareSize = 0.0284317f;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(SmokeColorIndicator != null, "Start: SmokeColorIndicator != null");
        Assert(HUDText_G != null, "Start: HUDText_G != null");
        Assert(HUDText_mach != null, "Start: HUDText_mach != null");
        Assert(HUDText_altitude != null, "Start: HUDText_altitude != null");
        Assert(HUDText_knotstarget != null, "Start: HUDText_knotstarget != null");
        Assert(HUDText_knots != null, "Start: HUDText_knots != null");
        Assert(HUDText_knotsairspeed != null, "Start: HUDText_knotsairspeed != null");
        Assert(HUDText_angleofattack != null, "Start: HUDText_angleofattack != null");
        Assert(HudCrosshairGun != null, "Start: HudCrosshairGun != null");
        Assert(HudCrosshair != null, "Start: HudCrosshair != null");
        Assert(HudLimit != null, "Start: HudLimit != null");
        Assert(HudAB != null, "Start: HudAB != null");
        Assert(DownIndicator != null, "Start: DownIndicator != null");
        Assert(ElevationIndicator != null, "Start: ElevationIndicator != null");
        Assert(HeadingIndicator != null, "Start: HeadingIndicator != null");
        Assert(VelocityIndicator != null, "Start: VelocityIndicator != null");
        Assert(AAMTargetIndicator != null, "Start: AAMTargetIndicator != null");
        Assert(LStickDisplayHighlighter != null, "Start: LStickDisplayHighlighter != null");
        Assert(RStickDisplayHighlighter != null, "Start: RStickDisplayHighlighter != null");
        Assert(PitchRoll != null, "Start: PitchRoll != null");
        Assert(Yaw != null, "Start: Yaw != null");
        Assert(TrimPitch != null, "Start: TrimPitch != null");
        Assert(TrimYaw != null, "Start: TrimYaw != null");
        Assert(AGMScreen != null, "Start: AGMScreen != null");
        Assert(LStick_funcon1 != null, "Start: LStick_funcon1 != null");
        Assert(LStick_funcon2 != null, "Start: LStick_funcon2 != null");
        Assert(LStick_funcon3 != null, "Start: LStick_funcon3 != null");
        Assert(LStick_funcon4 != null, "Start: LStick_funcon4 != null");
        Assert(LStick_funcon6 != null, "Start: LStick_funcon6 != null");
        Assert(LStick_funcon7 != null, "Start: LStick_funcon7 != null");
        Assert(LStick_funcon8 != null, "Start: LStick_funcon8 != null");
        Assert(RStick_funcon3 != null, "Start: LStick_funcon3 != null");
        Assert(RStick_funcon4 != null, "Start: LStick_funcon4 != null");
        Assert(RStick_funcon5 != null, "Start: LStick_funcon5 != null");
        Assert(RStick_funcon6 != null, "Start: LStick_funcon6 != null");
        Assert(RStick_funcon7 != null, "Start: LStick_funcon7 != null");

        PlaneAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        InputsZeroPos = PitchRoll.localPosition;
    }
    private void OnEnable()
    {
        maxGs = 0f;
    }
    private void Update()
    {
        //RollPitch Indicator
        PitchRoll.localPosition = InputsZeroPos + (new Vector3(-EngineControl.RollInput, EngineControl.PitchInput, 0)) * InputSquareSize;

        //Yaw Indicator
        Yaw.localPosition = InputsZeroPos + (new Vector3(EngineControl.YawInput, 0, 0)) * InputSquareSize;

        //Yaw Trim Indicator
        TrimYaw.localPosition = InputsZeroPos + (new Vector3(EngineControl.Trim.y, 0, 0)) * InputSquareSize;

        //Pitch Trim Indicator
        TrimPitch.localPosition = InputsZeroPos + (new Vector3(0, EngineControl.Trim.x, 0)) * InputSquareSize;

        //Velocity indicator
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

        if (EngineControl.RStickSelection == 1)
        {
            HudCrosshairGun.SetActive(true);
            HudCrosshair.SetActive(false);
        }
        else
        {
            HudCrosshairGun.SetActive(false);
            HudCrosshair.SetActive(true);
        }

        //AAM Target Indicator
        if (EngineControl.AAMHasTarget && EngineControl.RStickSelection == 2)
        {
            AAMTargetIndicator.localScale = new Vector3(1, 1, 1);
            AAMTargetIndicator.position = transform.position + EngineControl.AAMCurrentTargetDirection;
            AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
            if (EngineControl.AAMLock)
            {
                AAMTargetIndicator.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));//back of mesh is locked version
            }
            else
            {
                AAMTargetIndicator.localRotation = Quaternion.identity;
            }
        }
        else AAMTargetIndicator.localScale = Vector3.zero;
        /////////////////

        //Smoke Color Indicator
        SmokeColorIndicator.color = EngineControl.SmokeColor_Color;

        //Heading indicator
        temprot = EngineControl.VehicleMainObj.transform.rotation.eulerAngles;
        temprot.x = 0;
        temprot.z = 0;
        HeadingIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////

        //Elevation indicator
        temprot = EngineControl.VehicleMainObj.transform.rotation.eulerAngles;
        float new_z = temprot.z;
        temprot.y = 0;
        temprot.z = 0;
        ElevationIndicator.localRotation = Quaternion.Euler(-temprot);
        ElevationIndicator.RotateAround(ElevationIndicator.position, EngineControl.VehicleMainObj.transform.forward, -new_z);
        ElevationIndicator.localPosition = Vector3.zero;
        /////////////////

        //Down indicator
        DownIndicator.localRotation = Quaternion.Euler(new Vector3(0, 0, -new_z));
        /////////////////

        //SAFE indicator
        if (EngineControl.FlightLimitsEnabled)
        {
            HudLimit.SetActive(true);
        }
        else { HudLimit.SetActive(false); }

        //Left Stick Selector
        switch (EngineControl.LStickSelection)
        {
            case 0:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180);//invisible, backfacing
                break;
            case 1:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case 2:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 45, 0);
                break;
            case 3:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            case 4:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 135, 0);
                break;
            case 5:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 6:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 225, 0);
                break;
            case 7:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 270, 0);
                break;
            case 8:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 315, 0);
                break;
        }

        //Right Stick Selector
        switch (EngineControl.RStickSelection)
        {
            case 0:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 180);//invisible, backfacing
                break;
            case 1:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case 2:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 45, 0);
                break;
            case 3:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            case 4:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 135, 0);
                break;
            case 5:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 6:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 225, 0);
                break;
            case 7:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 270, 0);
                break;
            case 8:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 315, 0);
                break;
        }


        //AB
        if (EngineControl.EffectsControl.AfterburnerOn) { HudAB.SetActive(true); }
        else { HudAB.SetActive(false); }

        //Cruise Control target knots
        if (EngineControl.Cruise)
        {
            HUDText_knotstarget.text = ((EngineControl.SetSpeed) * 1.9438445f).ToString("F0");
        }
        else { HUDText_knotstarget.text = string.Empty; }

        //left stick toggles/functions on?
        if (EngineControl.Cruise) { LStick_funcon1.SetActive(true); }
        else { LStick_funcon1.SetActive(false); }

        if (EngineControl.FlightLimitsEnabled) { LStick_funcon2.SetActive(true); }
        else { LStick_funcon2.SetActive(false); }

        if (EngineControl.CatapultStatus == 1) { LStick_funcon3.SetActive(true); }
        else { LStick_funcon3.SetActive(false); }

        if (EngineControl.EffectsControl.HookDown) { LStick_funcon4.SetActive(true); }
        else { LStick_funcon4.SetActive(false); }

        if (EngineControl.Trim.x != 0) { LStick_funcon6.SetActive(true); }
        else { LStick_funcon6.SetActive(false); }

        if (EngineControl.EffectsControl.CanopyOpen) { LStick_funcon7.SetActive(true); }
        else { LStick_funcon7.SetActive(false); }

        if (EngineControl.EffectsControl.AfterburnerOn) { LStick_funcon8.SetActive(true); }
        else { LStick_funcon8.SetActive(false); }


        //right stick toggles/functions on?
        if (EngineControl.AGMLocked) { RStick_funcon3.SetActive(true); }
        else { RStick_funcon3.SetActive(false); }

        if (EngineControl.LevelFlight) { RStick_funcon4.SetActive(true); }
        else { RStick_funcon4.SetActive(false); }

        if (!EngineControl.EffectsControl.GearUp) { RStick_funcon5.SetActive(true); }
        else { RStick_funcon5.SetActive(false); }

        if (EngineControl.EffectsControl.Flaps) { RStick_funcon6.SetActive(true); }
        else { RStick_funcon6.SetActive(false); }

        if (EngineControl.EffectsControl.Smoking) { RStick_funcon7.SetActive(true); }
        else { RStick_funcon7.SetActive(false); }

        //play menu sound if selection changed since last frame
        float MenuSoundCheck = EngineControl.RStickSelection + EngineControl.LStickSelection;
        if (!EngineControl.SoundControl.MenuSelectNull && MenuSoundCheck != MenuSoundCheckLast)
        {
            EngineControl.SoundControl.MenuSelect.Play();
        }
        MenuSoundCheckLast = MenuSoundCheck;

        //AGMScreen
        if (EngineControl.RStickSelection == 3 && !EngineControl.AGMLocked)
        {
            if (EngineControl.AGMCam != null)
            {
                AGMScreen.SetActive(true);
                EngineControl.AGMCam.gameObject.SetActive(true);
                RaycastHit camhit;
                Physics.Raycast(EngineControl.AGMCam.transform.position, EngineControl.AGMCam.transform.forward, out camhit, Mathf.Infinity, 1);
                if (camhit.point != null)
                {
                    //dolly zoom //Mathf.Atan(100 <--the 100 is the height of the camera frustrum at the target distance
                    float newzoom = 0;
                    //zooming in is slower than zooming out
                    if (EngineControl.AGMRotDif < .2f)
                    {
                        newzoom = Mathf.Clamp(2.0f * Mathf.Atan(100 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1.5f, 90);
                    }
                    else
                    {
                        newzoom = 80;
                    }
                    EngineControl.AGMCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(EngineControl.AGMCam.fieldOfView, newzoom, 1.5f * Time.deltaTime), 0.3f, 90);
                }
            }
        }
        else if (EngineControl.AGMLocked)
        {
            if (EngineControl.AGMCam != null) EngineControl.AGMCam.transform.LookAt(EngineControl.AGMTarget, EngineControl.VehicleMainObj.transform.up);
            RaycastHit camhit;
            Physics.Raycast(EngineControl.AGMCam.transform.position, EngineControl.AGMCam.transform.forward, out camhit, Mathf.Infinity, 1);
            if (camhit.point != null)
            {
                //dolly zoom //Mathf.Atan(40 <--the 40 is the height of the camera frustrum at the target distance
                EngineControl.AGMCam.fieldOfView = Mathf.Max(Mathf.Lerp(EngineControl.AGMCam.fieldOfView, 2.0f * Mathf.Atan(60 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 5 * Time.deltaTime), 0.3f);
            }
        }
        else
        {
            if (EngineControl.AGMCam != null)
            {
                AGMScreen.SetActive(false);
                EngineControl.AGMCam.gameObject.SetActive(false);
            }
        }

        //updating numbers 3~ times a second
        if (check > .3)//update text
        {
            if (EngineControl.Gs > maxGs) { maxGs = EngineControl.Gs; }
            HUDText_G.text = string.Concat(EngineControl.Gs.ToString("F1"), "\n", maxGs.ToString("F1"));
            HUDText_mach.text = ((EngineControl.Speed) / 343f).ToString("F2");
            HUDText_altitude.text = string.Concat((EngineControl.CurrentVel.y * 60 * 3.28084f).ToString("F0"), "\n", ((EngineControl.CenterOfMass.position.y + -EngineControl.SeaLevel) * 3.28084f).ToString("F0"));
            HUDText_knots.text = ((EngineControl.Speed) * 1.9438445f).ToString("F0");
            HUDText_knotsairspeed.text = ((EngineControl.AirVel.magnitude) * 1.9438445f).ToString("F0");

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
        check += Time.deltaTime;


        HUDText_AAM_ammo.text = EngineControl.NumAAM.ToString("F0");
        HUDText_AGM_ammo.text = EngineControl.NumAGM.ToString("F0");

        PlaneAnimator.SetFloat("throttle", EngineControl.ThrottleInput);
        PlaneAnimator.SetFloat("fuel", EngineControl.Fuel / EngineControl.FullFuel);
        PlaneAnimator.SetFloat("gunammo", EngineControl.GunAmmoInSeconds / EngineControl.FullGunAmmo);
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}