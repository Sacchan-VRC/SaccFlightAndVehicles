﻿
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
            if (!SGVControl.IsOwner)
            {
                Vector3 VehicleVel = SGVControl.CurrentVel;
                if (VehicleVel != LastFrameVel)
                {
                    //calc Gs
                    float gravity = 9.81f * (Time.time - LastVelUpdate);
                    Vector3 gravity3 = Vector3.up * gravity;

                    Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel - gravity3);
                    AllGsRecieved = Gs3 / gravity;
                    LastFrameVel = VehicleVel;
                    LastVelUpdate = Time.time;
                }
                AllGsLocal = Vector3.Lerp(AllGsLocal, AllGsRecieved, 5f * Time.deltaTime);
            }

            if (HGs_txt) HGs_txt.text = Mathf.Abs(AllGsLocal.x).ToString("F1");
            AllGsLocal.x = Mathf.Clamp(AllGsLocal.x, -4, 4);
            if (FGs_txt) FGs_txt.text = Mathf.Abs(AllGsLocal.z).ToString("F1");
            AllGsLocal.z = Mathf.Clamp(AllGsLocal.z, -4, 4);
            Vector3 GPos = new Vector3(-AllGsLocal.z * 2.5f, 0, AllGsLocal.x * 2.5f);
            if (Gs_Marker) Gs_Marker.localPosition = GPos;

            float speed = (float)SGVControl.GetProgramVariable("VehicleSpeed");
            Speed_txt.text = (speed * SpeedMultiplier).ToString("F0");

            int gear = (SGVControl.CurrentGear - NumReverseGears);
            if (Gear_txt)
            {
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
            }
            Vector3 ClutchScale = new Vector3(1, 1, SGVControl.Clutch);
            if (ClutchBar) ClutchBar.localScale = ClutchScale;
        }
        float LastVelUpdate;
        private Vector3 AllGsRecieved;
        private Vector3 AllGsLocal;
        private Vector3 LastFrameVel;
        private void FixedUpdate()
        {
            if (!SGVControl.IsOwner) return;
            Vector3 VehicleVel = VehicleRigidbody.velocity;
            //calc Gs
            float gravity = 9.81f * Time.fixedDeltaTime;
            LastFrameVel.y -= gravity; //add gravity

            Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
            AllGsLocal = Gs3 / gravity;
            LastFrameVel = VehicleVel;
        }
    }
}