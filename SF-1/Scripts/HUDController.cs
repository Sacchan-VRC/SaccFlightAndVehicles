
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class HUDController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    private float maxGs = 0f;
    public Text HUDText_G;
    public Text HUDText_mach;
    public Text HUDText_altitude;
    public Text HUDText_knotstarget;
    public Text HUDText_knots;
    public Text HUDText_knotsairspeed;
    public Text HUDText_angleofattack;
    private string HUDText_angleofattack_temp;
    private const float distance_from_head = 1.333f;
    public Transform DownIndicator;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    public Transform VelocityIndicator;
    public Transform LStickDisplayHighlighter;
    public Transform RStickDisplayHighlighter;
    public Transform PitchRoll;
    public Transform Yaw;
    public Transform TrimPitch;
    public Transform TrimYaw;
    public GameObject HudSAFE;
    public GameObject HudAB;
    public GameObject AGMScreen;
    public GameObject AGMCam;
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
    private Vector3 InputsZeroPos;
    private Vector3 tempvel = Vector3.zero;
    private Vector3 startingpos;
    private float check = 0;
    [System.NonSerializedAttribute] [HideInInspector] public float MenuSoundCheckLast = 6;
    private Vector3 temprot;
    private int showvel;
    private void Start()
    {
        InputsZeroPos = PitchRoll.localPosition;
    }
    private void OnEnable()
    {
        maxGs = 0f;
    }
    private void Update()
    {
        const float InputSquareSize = 0.0284317f;
        //RollPitch Indicator
        PitchRoll.localPosition = InputsZeroPos + (new Vector3(-EngineControl.rollinput, EngineControl.pitchinput, 0)) * InputSquareSize;

        //Yaw Indicator
        Yaw.localPosition = InputsZeroPos + (new Vector3(EngineControl.yawinput, 0, 0)) * InputSquareSize;

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
        DownIndicator.localRotation = Quaternion.Euler(-new Vector3(0, 0, -new_z));
        /////////////////

        //SAFE indicator
        if (EngineControl.FlightLimitsEnabled)
        {
            HudSAFE.SetActive(true);
        }
        else { HudSAFE.SetActive(false); }

        //Left Stick Selector
        switch (EngineControl.LStickSelection)
        {
            case 0:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0);//invisible, backfacing
                break;
            case 1:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case 2:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -45);
                break;
            case 3:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -90);
                break;
            case 4:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -135);
                break;
            case 5:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -180);
                break;
            case 6:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -225);
                break;
            case 7:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -270);
                break;
            case 8:
                LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -315);
                break;
        }

        //Right Stick Selector
        switch (EngineControl.RStickSelection)
        {
            case 0:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0);//invisible, backfacing
                break;
            case 1:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case 2:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -45);
                break;
            case 3:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -90);
                break;
            case 4:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -135);
                break;
            case 5:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -180);
                break;
            case 6:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -225);
                break;
            case 7:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -270);
                break;
            case 8:
                RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -315);
                break;
        }


        //AB
        if (EngineControl.AfterburnerOn) { HudAB.SetActive(true); }
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

        if (EngineControl.HookDown) { LStick_funcon4.SetActive(true); }
        else { LStick_funcon4.SetActive(false); }

        if (EngineControl.Trim.x != 0) { LStick_funcon6.SetActive(true); }
        else { LStick_funcon6.SetActive(false); }

        if (EngineControl.CanopyOpen) { LStick_funcon7.SetActive(true); }
        else { LStick_funcon7.SetActive(false); }

        if (EngineControl.AfterburnerOn) { LStick_funcon8.SetActive(true); }
        else { LStick_funcon8.SetActive(false); }


        //right stick toggles/functions on?
        if (EngineControl.AGMLocked) { RStick_funcon3.SetActive(true); }
        else { RStick_funcon3.SetActive(false); }

        if (EngineControl.LevelFlight) { RStick_funcon4.SetActive(true); }
        else { RStick_funcon4.SetActive(false); }

        if (!EngineControl.GearUp) { RStick_funcon5.SetActive(true); }
        else { RStick_funcon5.SetActive(false); }

        if (EngineControl.Flaps) { RStick_funcon6.SetActive(true); }
        else { RStick_funcon6.SetActive(false); }

        if (EngineControl.Smoking) { RStick_funcon7.SetActive(true); }
        else { RStick_funcon7.SetActive(false); }

        //play menu sound if selection changed since last frame
        float MenuSoundCheck = EngineControl.RStickSelection + EngineControl.LStickSelection;
        if (MenuSoundCheck != MenuSoundCheckLast)
        {
            EngineControl.SoundControl.MenuSelect.Play();
        }
        MenuSoundCheckLast = MenuSoundCheck;

        //AGMScreen
        if (EngineControl.RStickSelection == 3)
        {
            AGMScreen.SetActive(true);
            EngineControl.AGMCam.SetActive(true);
        }
        else if (!EngineControl.AGMLocked)
        {
            AGMScreen.SetActive(false);
            EngineControl.AGMCam.SetActive(false);
        }

        //AGM Camera
        if (EngineControl.AGMLocked)
        {
            if (AGMCam != null) AGMCam.transform.LookAt(EngineControl.AGMTarget);
        }

        //updating numbers 3~x times a second
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
    }
}