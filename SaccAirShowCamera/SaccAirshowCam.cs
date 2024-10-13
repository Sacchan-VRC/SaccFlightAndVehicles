
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.Udon.Common;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(10000)]
    public class SaccAirshowCam : UdonSharpBehaviour
    {
        [SerializeField] Camera zoomCamera;
        [SerializeField] float CameraTurnResponse = 10f;
        [SerializeField] float Mouse_Sensitivity = 0.02f;
        [SerializeField] float CameraZoomSpeed = 1f;
        [SerializeField] float CameraZoomSmoothness = 6f;
        [SerializeField] float ShakyCamAmount = .33f;
        [SerializeField] float ShakyCamShakeSpeed = .33f;
        [SerializeField] float LockOnTransitionLength = 1f;
        [SerializeField] float cameraHeight = 1.5f;
        private bool ShakyCam = true;
        [SerializeField] Transform VehiclesParent;
        [SerializeField] Transform[] OtherTargets;
        [SerializeField] TextMeshProUGUI UITXT;
        [SerializeField] GameObject DisableIfVR;
        SaccEntity[] TargetVehicles;
        SaccAirVehicle TargetVehicle_SAV;
        Transform currentTarget;
        bool lockedIn;
        VRCPlayerApi localPlayer;
        Vector3 startPos;
        Quaternion startRot;

        private float CameraTurnResponse_Start, Mouse_Sensitivity_Start, CameraZoomSpeed_Start, ShakyCamAmount_Start, ShakyCamShakeSpeed_Start, LockOnTransitionLength_Start;
        void Start()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            CameraTurnResponse_Start = CameraTurnResponse;
            Mouse_Sensitivity_Start = Mouse_Sensitivity;
            CameraZoomSpeed_Start = CameraZoomSpeed;
            ShakyCamAmount_Start = ShakyCamAmount;
            ShakyCamShakeSpeed_Start = ShakyCamShakeSpeed;
            LockOnTransitionLength_Start = LockOnTransitionLength;

            if (VehiclesParent)
                TargetVehicles = VehiclesParent.GetComponentsInChildren<SaccEntity>();
            else
                Debug.LogWarning("SaccAirShowCam: VehiclesParent reference not set");
            localPlayer = Networking.LocalPlayer;
            zoomCamera.enabled = false;
            UITXT.transform.parent.gameObject.SetActive(false);
            if (localPlayer.IsUserInVR() && DisableIfVR) { DisableIfVR.SetActive(false); }
        }
        public override void OnPickupUseDown()
        {
            if (!zoomCamera.enabled) return;
            lockedIn = !lockedIn;
            float transistionPos = Time.time - transitionStartTime;
            float t = Mathf.Clamp01(transistionPos / LockOnTransitionLength);
            Transform lastTarget = currentTarget;
            if (lockedIn)
            {
                float lowestAngle = 9999999;
                for (int i = 0; i < TargetVehicles.Length; i++)
                {
                    float angle = Vector3.Angle(zoomCamera.transform.forward, TargetVehicles[i].transform.position - zoomCamera.transform.position);
                    float targetDistance = Vector3.Distance(TargetVehicles[i].CenterOfMass.position, zoomCamera.transform.position);
                    RaycastHit hit;
                    bool rayhit = Physics.Raycast(zoomCamera.transform.position, TargetVehicles[i].CenterOfMass.position - zoomCamera.transform.position, out hit, targetDistance, 2049 /* Default and Environment */, QueryTriggerInteraction.Ignore);
                    bool visible = (hit.distance / targetDistance) > 0.95f;
                    if (!rayhit || visible)
                    {
                        if (angle < lowestAngle)
                        {
                            lowestAngle = angle;
                            if (TargetVehicles[i].CenterOfMass)
                            {
                                currentTarget = TargetVehicles[i].CenterOfMass;
                                TargetVehicle_SAV = (SaccAirVehicle)TargetVehicles[i].GetExtention(GetUdonTypeName<SaccAirVehicle>());
                            }
                            else
                            {
                                currentTarget = TargetVehicles[i].transform;
                                TargetVehicle_SAV = null;
                            }
                        }
                    }
                }
                for (int i = 0; i < OtherTargets.Length; i++)
                {
                    float angle = Vector3.Angle(zoomCamera.transform.forward, OtherTargets[i].position - zoomCamera.transform.position);
                    float targetDistance = Vector3.Distance(OtherTargets[i].transform.position, zoomCamera.transform.position);
                    RaycastHit hit;
                    bool rayhit = Physics.Raycast(zoomCamera.transform.position, OtherTargets[i].transform.position - zoomCamera.transform.position, out hit, targetDistance, 2049 /* Default and Environment */, QueryTriggerInteraction.Ignore);
                    bool visible = ((hit.distance / targetDistance) > 0.95f) || !rayhit;
                    if (!rayhit || visible)
                    {
                        if (angle < lowestAngle)
                        {
                            lowestAngle = angle;
                            currentTarget = OtherTargets[i];
                            TargetVehicle_SAV = null;
                        }
                    }
                }
                if (currentTarget == lastTarget)
                {
                    transitionStartTime = Time.time - (LockOnTransitionLength * (1 - t));
                }
                else
                {
                    FreeLook = FreeLookSlerped = currentTargetDir_Quat;
                    FreeLook3 = FreeLook.eulerAngles;
                    if (FreeLook3.x > 180) { FreeLook3.x -= 360; }
                    if (FreeLook3.y > 180) { FreeLook3.y -= 360; }
                    if (FreeLook3.z > 180) { FreeLook3.z -= 360; }
                    transitionStartTime = Time.time;
                }
            }
            else
            {
                transitionStartTime = Time.time - (LockOnTransitionLength * (1 - t));
                if (t >= 1f)
                {
                    FreeLook = FreeLookSlerped = currentTargetDir_Quat;
                    FreeLook3 = FreeLook.eulerAngles;
                    if (FreeLook3.x > 180) { FreeLook3.x -= 360; }
                    if (FreeLook3.y > 180) { FreeLook3.y -= 360; }
                    if (FreeLook3.z > 180) { FreeLook3.z -= 360; }
                }
                localPlayer.TeleportTo(localPlayer.GetPosition(), zoomCamera.transform.rotation, VRC_SceneDescriptor.SpawnOrientation.Default, true);
            }
        }
        private float transitionStartTime;
        public override void OnPickupUseUp()
        {
        }
        public override void OnPickup()
        {
            held = true;
            UITXT.transform.parent.gameObject.SetActive(true);
        }

        public override void OnDrop()
        {
            freezePlayer = false;
            zoomCamera.enabled = lockedIn = held = false;
            UITXT.transform.parent.gameObject.SetActive(false);
        }
        bool held;
        float targetFov = 70f;
        float fieldOfView_smoothdamp;
        Quaternion FreeLookSlerped;
        float ShakeSeedX;
        float ShakeSeedY;
        bool UI_ON = true;
        Vector3 targetPosLast;
        Vector3 targetVelocity;//unused
        bool freezePlayer;
        Quaternion FreeLook;
        Vector3 FreeLook3;
        Vector3 freezePos;
        Quaternion lastRot;
        Quaternion Rot_Rate;
        Quaternion currentTargetDir_Quat;
        void LateUpdate()
        {
            if (!held) return;
            if (Input.GetKeyDown(KeyCode.F1)) { UI_ON = !UI_ON; }

            if (Input.GetKeyDown(KeyCode.G))
            {
                freezePos = localPlayer.GetPosition();
                freezePlayer = !freezePlayer;
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                lockedIn = false;
                fieldOfView_smoothdamp = 0f;
                zoomCamera.fieldOfView = targetFov = 70f;
                zoomCamera.enabled = !zoomCamera.enabled;
                FreeLook = FreeLookSlerped = zoomCamera.transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                FreeLook3 = FreeLook.eulerAngles;
                if (FreeLook3.x > 180) { FreeLook3.x -= 360; }
                if (FreeLook3.y > 180) { FreeLook3.y -= 360; }
                FreeLook3.z = 0;
            }
            bool Shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float Shiftf = Shift ? 3f : 1;
            float Equalsf = Input.GetKey(KeyCode.Equals) ? 1 : 0;
            float Minusf = Input.GetKey(KeyCode.Minus) ? -1 : 0;
            ShakyCamAmount += (Equalsf + Minusf) * 1.5f * Time.deltaTime * Shiftf;
            ShakyCamAmount = Mathf.Max(0, ShakyCamAmount);
            float RightBracketf = Input.GetKey(KeyCode.RightBracket) ? 1 : 0;
            float LeftBracketf = Input.GetKey(KeyCode.LeftBracket) ? -1 : 0;
            ShakyCamShakeSpeed += (RightBracketf + LeftBracketf) * 1.5f * Time.deltaTime * Shiftf;
            ShakyCamShakeSpeed = Mathf.Max(0, ShakyCamShakeSpeed);
            float Alpha0f = Input.GetKey(KeyCode.Alpha0) ? 1 : 0;
            float Alpha9f = Input.GetKey(KeyCode.Alpha9) ? -1 : 0;
            Mouse_Sensitivity += (Alpha0f + Alpha9f) * 0.05f * Time.deltaTime * Shiftf;
            Mouse_Sensitivity = Mathf.Max(0f, Mouse_Sensitivity);
            float Alpha8f = Input.GetKey(KeyCode.Alpha8) ? 1 : 0;
            float Alpha7f = Input.GetKey(KeyCode.Alpha7) ? -1 : 0;
            CameraTurnResponse += (Alpha8f + Alpha7f) * 3f * Time.deltaTime * Shiftf;
            CameraTurnResponse = Mathf.Max(0.1f, CameraTurnResponse);
            float Pf = Input.GetKey(KeyCode.P) ? 1 : 0;
            float Of = Input.GetKey(KeyCode.O) ? -1 : 0;
            LockOnTransitionLength += (Pf + Of) * 0.5f * Time.deltaTime * Shiftf;
            LockOnTransitionLength = Mathf.Max(0.01f, LockOnTransitionLength);
            float Alpha6f = Input.GetKey(KeyCode.Alpha6) ? 1 : 0;
            float Alpha5f = Input.GetKey(KeyCode.Alpha5) ? -1 : 0;
            cameraHeight += (Alpha6f + Alpha5f) * 2f * Time.deltaTime * Shiftf;
            cameraHeight = Mathf.Max(0f, cameraHeight);
            if (Input.GetKeyDown(KeyCode.Home) && Shift) { ResetValues(); }
            if (Input.GetKeyDown(KeyCode.H)) { ShakyCam = !ShakyCam; }

            if (freezePlayer)
            {
                localPlayer.SetVelocity(Vector3.zero);

                localPlayer.TeleportTo(freezePos, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation, VRC_SceneDescriptor.SpawnOrientation.Default, true);
            }
            if (!zoomCamera.enabled) { UpdateUI(); return; }
            float MouseX = Input.GetAxisRaw("Mouse X");
            float MouseY = Input.GetAxisRaw("Mouse Y");
            FreeLook3.x -= MouseY * Mouse_Sensitivity * zoomCamera.fieldOfView;
            FreeLook3.x = Mathf.Clamp(FreeLook3.x, -89.9f, 89.9f);
            FreeLook3.y += MouseX * Mouse_Sensitivity * zoomCamera.fieldOfView;
            FreeLook = Quaternion.Euler(FreeLook3);
            zoomCamera.transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + Vector3.up * cameraHeight;
            float camsmoothness = CameraTurnResponse/*  / zoomCamera.fieldOfView */;
            FreeLookSlerped = Quaternion.Slerp(FreeLookSlerped, FreeLook, 1 - Mathf.Pow(0.5f, Time.deltaTime * camsmoothness));
            currentTargetDir_Quat = FreeLookSlerped;
            float Ef = Input.GetKey(KeyCode.E) ? -1 : 0;
            float Qf = Input.GetKey(KeyCode.Q) ? 1 : 0;
            targetFov += (Ef + Qf) * CameraZoomSpeed * Time.deltaTime * zoomCamera.fieldOfView * Shiftf;
            targetFov = Mathf.Clamp(targetFov, 0.1f, 100f);
            zoomCamera.fieldOfView = Mathf.SmoothDamp(zoomCamera.fieldOfView, targetFov, ref fieldOfView_smoothdamp, 1 - Mathf.Pow(0.5f, Time.deltaTime * CameraZoomSmoothness));

            if (currentTarget)
            {
                currentTargetDir_Quat = Quaternion.LookRotation(currentTarget.position - zoomCamera.transform.position);
                if (TargetVehicle_SAV)
                {
                    targetVelocity = (currentTarget.position - targetPosLast).normalized * TargetVehicle_SAV.AirSpeed;
                }
                else
                {
                    targetVelocity = (currentTarget.position - targetPosLast) / Time.deltaTime;
                }
                targetPosLast = currentTarget.position;
            }
            float transistionPos = Time.time - transitionStartTime;
            float t = transistionPos / LockOnTransitionLength;
            if (lockedIn)
            {
                currentTargetDir_Quat = Quaternion.Slerp(FreeLookSlerped, currentTargetDir_Quat, t);
            }
            else
            {
                currentTargetDir_Quat = Quaternion.Slerp(currentTargetDir_Quat, FreeLookSlerped, t);
            }
            Quaternion CamShake = Quaternion.identity;
            if (ShakyCam)
            {
                ShakeSeedX += Time.deltaTime * ShakyCamShakeSpeed;
                ShakeSeedY += Time.deltaTime * ShakyCamShakeSpeed;
                float shakeX = Mathf.PerlinNoise1D(ShakeSeedX) - 0.5f;
                float shakY = Mathf.PerlinNoise1D(ShakeSeedY + 53.5f) - 0.5f;
                Quaternion shakeXQ = Quaternion.AngleAxis(shakeX * ShakyCamAmount, currentTargetDir_Quat * Vector3.right);
                Quaternion shakeYQ = Quaternion.AngleAxis(shakY * ShakyCamAmount, currentTargetDir_Quat * Vector3.up);
                CamShake = shakeXQ;
                CamShake = CamShake * shakeYQ;
            }

            zoomCamera.transform.rotation = KillRoll(CamShake * currentTargetDir_Quat);

            localPlayer.TeleportTo(localPlayer.GetPosition(), currentTargetDir_Quat, VRC_SceneDescriptor.SpawnOrientation.Default, true);

            Quaternion Rotdiff = currentTargetDir_Quat * Quaternion.Inverse(lastRot);
            // Rotdiff = Quaternion.SlerpUnclamped(Quaternion.identity, Rotdiff, 1f / Time.deltaTime);
            Rot_Rate = Quaternion.Slerp(Rot_Rate, Rotdiff, 1 - Mathf.Pow(0.5f, 8f));
            lastRot = currentTargetDir_Quat;

            UpdateUI();
        }
        Quaternion KillRoll(Quaternion inquat)
        {
            Vector3 eulerangs = inquat.eulerAngles;
            if (eulerangs.x > 180) { eulerangs.x -= 360; }
            if (eulerangs.y > 180) { eulerangs.y -= 360; }
            eulerangs.z = 0;
            // if (eulerangs.z > 180) { eulerangs.z -= 360; }
            return Quaternion.Euler(eulerangs);
        }
        void ResetValues()
        {
            CameraTurnResponse = CameraTurnResponse_Start;
            Mouse_Sensitivity = Mouse_Sensitivity_Start;
            CameraZoomSpeed = CameraZoomSpeed_Start;
            ShakyCamAmount = ShakyCamAmount_Start;
            ShakyCamShakeSpeed = ShakyCamShakeSpeed_Start;
            LockOnTransitionLength = LockOnTransitionLength_Start;
            ShakyCam = true;
        }
        void UpdateUI()
        {
            if (UI_ON)
            {
                UITXT.text =
                "F1 =<indent=15%>Toggle UI</indent>" +
                "\nX =<indent=15%>Toggle camera</indent>" +
                "\nLeft Click =<indent=15%>Lock/Unlock target</indent>" +
                "\nE/Q =<indent=15%>Zoom</indent>" +
                "\nSHIFT =<indent=15%>Zoom Speed 3X</indent>" +
                "\n0/9 =<indent=15%>Mouse Sens</indent>" +
                "\n8/7 =<indent=15%>Camera Turn Response</indent>" +
                "\n6/5 =<indent=15%>Camera Height</indent>" +
                "\no/p =<indent=15%>Lock Transition Length</indent>" +
                "\nH =<indent=15%>Toggle Shaky Cam</indent>" +
                "\n=/- =<indent=15%>Shake Amount</indent>" +
                "\n]/[ =<indent=15%>Shake Speed</indent>" +
                "\nG =<indent=15%>Freeze Player</indent>" +
                "\nShift+Home =<indent=15%>Reset All</indent>" +

                "\n\nCamera: " + (zoomCamera.enabled ? "ENABLED" : "DISABLED") +
                "\nCamera FOV: " + zoomCamera.fieldOfView.ToString("F2") +
                "\nMouse Sens: " + Mouse_Sensitivity.ToString("F3") +
                "\nCamera Turn Response: " + CameraTurnResponse.ToString("F2") +
                "\nCamera Height: " + cameraHeight.ToString("F2") +
                "\nLock Transition Length: " + LockOnTransitionLength.ToString("F2") +
                "\nCamera Locked: " + (lockedIn ? "YES" : "NO") +
                "\nPlayer Frozen: " + (freezePlayer ? "YES" : "NO") +
                "\nShaky Cam: " + (ShakyCam ? "ON" : "OFF") +
                "\nCamera Shake Amount: " + ShakyCamAmount.ToString("F2") +
                "\nCamera Shake Speed: " + ShakyCamShakeSpeed.ToString("F2")
                ;
            }
            else { UITXT.text = string.Empty; }
        }
        public void Respawn()
        {
            transform.position = startPos;
            transform.rotation = startRot;
        }
    }
}