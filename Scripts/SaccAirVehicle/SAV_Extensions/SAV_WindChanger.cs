//comented stuff can be used to make changes in wind global
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_WindChanger : UdonSharpBehaviour
{
    [Tooltip("List of SaccAirVehicles to be effected by this WindChnager")]
    public UdonSharpBehaviour[] SaccAirVehicles;
    public GameObject WindMenu;
    public Slider WindStrengthSlider;
    public Text WindStr_text;
    public Slider WindGustStrengthSlider;
    public Text WindGustStrength_text;
    public Slider WindGustinessSlider;
    public Text WindGustiness_text;
    public Slider WindTurbulanceScaleSlider;
    public Text WindTurbulanceScale_text;
    public AudioSource WindApplySound;
    /* [UdonSynced(UdonSyncMode.None)] */
    private float WindStrength;
    /* [UdonSynced(UdonSyncMode.None)] */
    private float WindGustStrength;
    /* [UdonSynced(UdonSyncMode.None)] */
    private float WindGustiness;
    /* [UdonSynced(UdonSyncMode.None)] */
    private float WindTurbulanceScale;
    private VRCPlayerApi localPlayer;
    private bool menuactive;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }
    private void Update()
    {
        /*    if (localPlayer.IsOwner(gameObject))
           { */
        if (menuactive)
        {
            WindStrength = WindStrengthSlider.value;
            WindStr_text.text = WindStrengthSlider.value.ToString("F1");

            WindGustStrength = WindGustStrengthSlider.value;
            WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");

            WindGustiness = WindGustinessSlider.value;
            WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");

            WindTurbulanceScale = WindTurbulanceScaleSlider.value;
            WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");

            if (Vector3.Distance(transform.position, localPlayer.GetPosition()) > 3)
            {
                WindMenu.SetActive(false);
                menuactive = false;
            }
        }
        /*    }
           else
           {
               WindStrengthSlider.value = WindStrength;
               WindGustStrengthSlider.value = WindGustStrength;
               WindGustinessSlider.value = WindGustiness;
               WindTurbulanceScaleSlider.value = WindTurbulanceScale;
           } */
    }
    public override void OnPickup()
    {
        WindMenu.SetActive(true);
        menuactive = true;
    }
    public override void OnPickupUseDown()
    {
        ApplyWindDir();
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ApplyWindDir");
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 NewWindDir = (gameObject.transform.rotation * Vector3.forward) * WindStrength;
        foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
        {
            if (vehicle)
            {
                vehicle.SetProgramVariable("Wind", NewWindDir);
                vehicle.SetProgramVariable("WindGustStrength", WindGustStrength);
                vehicle.SetProgramVariable("WindGustiness", WindGustiness);
                vehicle.SetProgramVariable("WindTurbulanceScale", WindTurbulanceScale);
            }
        }
    }
    /*     private void OnPickup()
        {
            WindMenu.SetActive(true);
        }

        private void OnOwnershipTransferred()
        {
            WindMenu.SetActive(false);
        } */
}
