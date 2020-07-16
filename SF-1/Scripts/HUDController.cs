
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
    public Text HUDText_knots;
    public Text HUDText_angleofattack;
    private string HUDText_angleofattack_temp;
    public float distance_from_head = 1.333f;
    public Transform DownIndicator;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    public Transform VelocityIndicator;
    private Vector3 tempvel = Vector3.zero;
    private Vector3 startingpos;
    float check = 0;
    Vector3 temprot;
    private void OnEnable()
    {
        maxGs = 0f;
    }
    private void Update()
    {
        //Velocity indicator
        if (EngineControl.CurrentVel.magnitude < 2)
        {
            tempvel = -Vector3.up * 2;//straight down instead of spazzing out when moving very slow
        }
        else
        {
            tempvel = EngineControl.CurrentVel;
        }
        VelocityIndicator.position = (transform.position + tempvel);
        VelocityIndicator.localPosition = VelocityIndicator.localPosition.normalized * (distance_from_head);
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
        temprot = EngineControl.VehicleMainObj.transform.transform.rotation.eulerAngles;
        temprot.x = 0;
        temprot.y = 0;
        DownIndicator.localRotation = Quaternion.Euler(-temprot);
        /////////////////

        if (check > .3)//update text
        {
            if (EngineControl.Gs > maxGs) { maxGs = EngineControl.Gs; }
            HUDText_G.text = string.Concat(EngineControl.Gs.ToString("F1"), "\n", maxGs.ToString("F1"));
            HUDText_mach.text = ((EngineControl.CurrentVel.magnitude) / 343f).ToString("F2");
            HUDText_altitude.text = string.Concat((EngineControl.CurrentVel.y * 60 * 3.28084f).ToString("F0"), "\n", ((EngineControl.CenterOfMass.position.y + -EngineControl.SeaLevel) * 3.28084f).ToString("F0"));
            HUDText_knots.text = ((EngineControl.CurrentVel.magnitude) * 1.9438445f).ToString("F0");

            if (EngineControl.CurrentVel.magnitude < 2)
            {
                HUDText_angleofattack_temp = System.String.Empty;
            }
            else
            {
                HUDText_angleofattack_temp = EngineControl.AngleOfAttack.ToString("F0");
            }
            HUDText_angleofattack.text = HUDText_angleofattack_temp;
            check = 0;
            //  }
        }
        check += Time.deltaTime;
    }
}