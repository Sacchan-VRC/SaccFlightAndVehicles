//comented stuff can be used to make changes in wind global
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindChanger : UdonSharpBehaviour
{
    [SerializeField] private GameObject WindMenu;
    [SerializeField] private Slider WindStrengthSlider;
    [SerializeField] private Text WindStr_text;
    [SerializeField] private Slider WindGustStrengthSlider;
    [SerializeField] private Text WindGustStrength_text;
    [SerializeField] private Slider WindGustinessSlider;
    [SerializeField] private Text WindGustiness_text;
    [SerializeField] private Slider WindTurbulanceScaleSlider;
    [SerializeField] private Text WindTurbulanceScale_text;
    [SerializeField] private AudioSource WindApplySound;
    [SerializeField] private EngineController[] VehicleEngines;
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
    private void OnPickup()
    {
        WindMenu.SetActive(true);
        menuactive = true;
    }
    private void OnPickupUseDown()
    {
        ApplyWindDir();
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ApplyWindDir");
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 NewWindDir = (gameObject.transform.rotation * Vector3.forward) * WindStrength;
        foreach (EngineController vehicle in VehicleEngines)
        {
            if (vehicle != null)
            {
                vehicle.Wind = NewWindDir;
                vehicle.WindGustStrength = WindGustStrength;
                vehicle.WindGustiness = WindGustiness;
                vehicle.WindTurbulanceScale = WindTurbulanceScale;
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
