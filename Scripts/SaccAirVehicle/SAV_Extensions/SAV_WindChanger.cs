using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SAV_WindChanger : UdonSharpBehaviour
    {
        public bool DefaultSynced = false;
        [HideInInspector] public UdonSharpBehaviour[] SaccAirVehicles;
        public GameObject WindMenu;
        public Slider WindStrengthSlider;
        public Text WindStr_text;
        public Slider WindGustStrengthSlider;
        public Text WindGustStrength_text;
        public Slider WindGustinessSlider;
        public Text WindGustiness_text;
        public Slider WindTurbulanceScaleSlider;
        public Text WindTurbulanceScale_text;
        public Toggle WindSyncedToggle;
        public AudioSource WindApplySound;
        [UdonSynced] private Vector3 WindStrenth_3;
        [UdonSynced] private float WindGustStrength;
        [UdonSynced] private float WindGustiness = 0.03f;
        [UdonSynced] private float WindTurbulanceScale = 0.0001f;
        private float WindStrengthLocal;
        private float WindGustStrengthLocal;
        private float WindGustinessLocal = 0.03f;
        private float WindTurbulanceScaleLocal = 0.0001f;
        private VRCPlayerApi localPlayer;
        private bool menuactive;
        private bool SyncedWind = false;
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            WindMenu.SetActive(false);
            if (DefaultSynced)
            {
                SendCustomEventDelayedSeconds(nameof(ToggleSyncedWind), 5);
            }
        }
        public void ToggleSyncedWind()
        {
            SyncedWind = !SyncedWind;
            if (WindSyncedToggle && WindSyncedToggle.isOn != SyncedWind) WindSyncedToggle.SetIsOnWithoutNotify(SyncedWind);
            OnDeserialization();
        }
        public void UpdateValuesFromOther()
        {
            WindStrengthLocal = WindStrenth_3.magnitude;
            WindStrengthSlider.value = WindStrengthLocal;
            WindStr_text.text = WindStrengthLocal.ToString("F1");
            WindGustStrengthLocal = WindGustStrength;
            WindGustStrengthSlider.value = WindGustStrength;
            WindGustStrength_text.text = WindGustStrength.ToString("F1");
            WindGustinessLocal = WindGustiness;
            WindGustinessSlider.value = WindGustiness;
            WindGustiness_text.text = WindGustiness.ToString("F3");
            WindTurbulanceScaleLocal = WindTurbulanceScale;
            WindTurbulanceScaleSlider.value = WindTurbulanceScale;
            WindTurbulanceScale_text.text = WindTurbulanceScale.ToString("F5");

            if (WindStrenth_3.sqrMagnitude > 0)
                transform.rotation = Quaternion.FromToRotation(Vector3.forward, WindStrenth_3.normalized);
        }
        public void UpdateValues()
        {
            WindStrengthLocal = WindStrengthSlider.value;
            WindStr_text.text = WindStrengthSlider.value.ToString("F1");

            WindGustStrengthLocal = WindGustStrengthSlider.value;
            WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");

            WindGustinessLocal = WindGustinessSlider.value;
            WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");

            WindTurbulanceScaleLocal = WindTurbulanceScaleSlider.value;
            WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");
        }
        public void ProximityDisableLoop()
        {
            if (menuactive)
            {
                if (Vector3.Distance(transform.position, localPlayer.GetPosition()) > 5)
                {
                    WindMenu.SetActive(false);
                    menuactive = false;
                }
                else SendCustomEventDelayedSeconds(nameof(ProximityDisableLoop), 1);
            }
        }
        public override void OnPickup()
        {
            if (!menuactive)
            {
                menuactive = true;
                ProximityDisableLoop();
            }
            WindMenu.SetActive(true);
        }
        public override void OnPickupUseDown()
        {
            if (SyncedWind)
            {
                if (!Networking.LocalPlayer.IsOwner(gameObject))
                { Networking.SetOwner(Networking.LocalPlayer, gameObject); }
                UpdateValues();
                ApplyWindDir();
                WindStrenth_3 = transform.forward * WindStrengthLocal;
                WindGustStrength = WindGustStrengthLocal;
                WindGustiness = WindGustinessLocal;
                WindTurbulanceScale = WindTurbulanceScaleLocal;
                RequestSerialization();
            }
            else
            {
                UpdateValues();
                ApplyWindDir();
            }
        }
        public void WindSound()
        {
            if (!WindApplySound.isPlaying && Time.time > 8)
            { WindApplySound.Play(); }
        }
        public void ApplyWindDir()
        {
            WindApplySound.Play();
            Vector3 NewWindDir = transform.forward * WindStrengthLocal;
            foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
            {
                if (vehicle)
                {
                    vehicle.SetProgramVariable("Wind", NewWindDir);
                    vehicle.SetProgramVariable("WindGustStrength", WindGustStrengthLocal);
                    vehicle.SetProgramVariable("WindGustiness", WindGustinessLocal);
                    vehicle.SetProgramVariable("WindTurbulanceScale", WindTurbulanceScaleLocal);
                }
            }
        }
        public override void OnDeserialization()
        {
            if (SyncedWind)
            {
                UpdateValuesFromOther();
                ApplyWindDir();
            }
        }
    }
}