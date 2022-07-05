
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(1200)]//after wheels
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SGV_Gauges : UdonSharpBehaviour
    {
        public Transform VehicleTransform;
        private SaccGroundVehicle SGVControl;
        private SGV_GearBox GearBoxControl;
        private Rigidbody VehicleRigidbody;
        [Tooltip("M/s multiplied by")]
        public float SpeedMultiplier = 3.6f;
        public Transform Gs_Marker;
        public Transform ClutchBar;
        public Text HGs_txt;
        public Text FGs_txt;
        public Text Gear_txt;
        public Text Speed_txt;
        private int NumReverseGears = 0;
        private bool MoreThanOneRevrseGear = false;
        void Start()
        {
            VehicleRigidbody = VehicleTransform.GetComponent<Rigidbody>();
            SGVControl = (SaccGroundVehicle)((SaccEntity)VehicleTransform.GetComponent<SaccEntity>()).GetExtention(GetUdonTypeName<SaccGroundVehicle>());
            GearBoxControl = (SGV_GearBox)((SaccEntity)VehicleTransform.GetComponent<SaccEntity>()).GetExtention(GetUdonTypeName<SGV_GearBox>());
            if (GearBoxControl)
            {
                for (int i = 0; i < GearBoxControl.GearRatios.Length; i++)
                {
                    if (GearBoxControl.GearRatios[i] < 0)
                    {
                        NumReverseGears++;
                    }
                    else { continue; }
                }
                if (NumReverseGears > 1) { MoreThanOneRevrseGear = true; }
            }
        }
        private void LateUpdate()
        {
            HGs_txt.text = Mathf.Abs(AllGsLocal.x).ToString("F1");
            AllGsLocal.x = Mathf.Clamp(AllGsLocal.x, -4, 4);
            FGs_txt.text = Mathf.Abs(AllGsLocal.z).ToString("F1");
            AllGsLocal.z = Mathf.Clamp(AllGsLocal.z, -4, 4);
            Vector3 GPos = new Vector3(-AllGsLocal.z * 2.5f, 0, AllGsLocal.x * 2.5f);
            Gs_Marker.localPosition = GPos;

            float speed = (float)SGVControl.GetProgramVariable("VehicleSpeed");
            Speed_txt.text = (speed * SpeedMultiplier).ToString("F0");

            int gear = (SGVControl.CurrentGear - NumReverseGears);
            if (gear < 1)
            {
                if (gear == 0) { Gear_txt.text = "N"; }
                else
                {
                    if (MoreThanOneRevrseGear)
                    {
                        Gear_txt.text = string.Concat("R", (-gear).ToString("F0"));
                    }
                    else
                    {
                        Gear_txt.text = "R";
                    }
                }
            }
            else
            {
                Gear_txt.text = (gear).ToString("F0");
            }
            Vector3 ClutchScale = new Vector3(1, 1, SGVControl.Clutch);
            ClutchBar.localScale = ClutchScale;
        }
        private float AllGs;
        private Vector3 AllGsLocal;
        private Vector3 LastFrameVel;
        private void FixedUpdate()
        {
            float DeltaTime = Time.fixedDeltaTime;
            Vector3 VehicleVel = VehicleRigidbody.velocity;
            //calc Gs
            float gravity = 9.81f * DeltaTime;
            LastFrameVel.y -= gravity; //add gravity
            AllGs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            //GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);

            Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
            AllGsLocal = Gs3 / gravity;
            LastFrameVel = VehicleVel;
        }
    }
}