
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAAG_HUDController : UdonSharpBehaviour
    {
        [Tooltip("Transform of the pilot seat's target eye position, HUDContrller is automatically moved to this position in Start() to ensure perfect alignment")]
        public Transform PilotSeatAdjusterTarget;
        public SaccAAGunController AAGunControl;
        public Transform ElevationIndicator;
        public Transform HeadingIndicator;
        public Transform AAMTargetIndicator;
        public Transform GUNLeadIndicator;
        [Range(0.01f, 1)]
        [Tooltip("1 = max accuracy, 0.01 = smooth but innacurate")]
        [SerializeField] private float GunLeadResponsiveness = 1f;
        public Transform AAMReloadBar;
        public Transform MGAmmoBar;
        public Text HUDText_AAM_ammo;
        [Tooltip("Local distance projected forward for objects that move dynamically, only adjust if the hud is moved forward in order to make it appear smaller")]
        public float distance_from_head = 1.333f;
        [System.NonSerialized] public Vector3 GUN_TargetDirOld;
        [System.NonSerialized] public float GUN_TargetSpeedLerper;
        [System.NonSerialized] public Vector3 RelativeTargetVel;
        [System.NonSerialized] public Vector3 RelativeTargetVelLastFrame;
        private SaccEntity EntityControl;
        public float BulletSpeed = 1050;
        private float BulletSpeedDivider;
        private float AAMReloadBarDivider;
        private float MGReloadBarDivider;
        private Transform Rotator;
        private Quaternion backfacing = Quaternion.Euler(new Vector3(0, 180, 0));
        bool Initialized = false;
        public void RemoteInit() { Start(); }
        private void Start()
        {
            if (Initialized) return;
            Initialized = true;
            BulletSpeedDivider = 1f / BulletSpeed;
            AAMReloadBarDivider = 1f / AAGunControl.MissileReloadTime;
            MGReloadBarDivider = 1f / AAGunControl.MGAmmoSeconds;
            if (PilotSeatAdjusterTarget) { transform.position = PilotSeatAdjusterTarget.position; }

            Rotator = AAGunControl.Rotator.transform;

            EntityControl = AAGunControl.EntityControl;
        }
        private void Update()
        {
            float SmoothDeltaTime = Time.smoothDeltaTime;
            //AAM Target Indicator
            if (AAMTargetIndicator)
            {
                if (AAGunControl.AAMHasTarget)
                {
                    AAMTargetIndicator.gameObject.SetActive(true);
                    AAMTargetIndicator.position = transform.position + AAGunControl.AAMCurrentTargetDirection;
                    AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
                    AAMTargetIndicator.localRotation = Quaternion.identity;
                    if (AAGunControl.AAMLocked)
                    {
                        AAMTargetIndicator.localRotation = backfacing;//back of mesh is locked version
                    }
                }
                else
                {
                    AAMTargetIndicator.gameObject.SetActive(false);
                }
            }
            /////////////////

            //AAMs
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = AAGunControl.NumAAM.ToString("F0"); }
            /////////////////

            //AAM Reload bar
            if (AAMReloadBar) { AAMReloadBar.localScale = new Vector3(AAGunControl.AAMReloadTimer * AAMReloadBarDivider, .3f, 0); }
            /////////////////

            //MG Reload bar
            if (MGAmmoBar) { MGAmmoBar.localScale = new Vector3(AAGunControl.MGAmmoSeconds * MGReloadBarDivider, .3f, 0); }
            /////////////////

            //GUN Lead Indicator
            // GUNLead();
            /////////////////

            //Heading indicator
            Vector3 newrot = new Vector3(0, Rotator.rotation.eulerAngles.y, 0);
            if (HeadingIndicator)
            {
                HeadingIndicator.localRotation = Quaternion.Euler(-newrot);
            }
            /////////////////

            //Elevation indicator
            if (ElevationIndicator)
            { ElevationIndicator.rotation = Quaternion.Euler(newrot); }
            /////////////////
        }
        public void GUNLead()
        {
            if (GUNLeadIndicator)
            {
                //GUN Lead Indicator
                float deltaTime = Time.deltaTime;
                Vector3 HudControlPosition = transform.position;
                GUNLeadIndicator.gameObject.SetActive(true);
                Vector3 TargetPos;
                if (!AAGunControl.AAMCurrentTargetSAVControl)//target is a dummy target
                { TargetPos = AAGunControl.AAMTargets[AAGunControl.AAMTarget].transform.position; }
                else
                { TargetPos = AAGunControl.AAMCurrentTargetSAVControl.CenterOfMass.position; }
                Vector3 TargetDir = TargetPos - HudControlPosition;

                Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;

                GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, GunLeadResponsiveness);
                // GUN_TargetSpeedLerper = Mathf.Lerp(GUN_TargetSpeedLerper, (RelativeTargetVel.magnitude * GunLeadResponsiveness) / deltaTime, 15 * deltaTime);
                GUN_TargetSpeedLerper = RelativeTargetVel.magnitude * GunLeadResponsiveness / deltaTime;

                float interceptTime = vintercept(HudControlPosition, BulletSpeed, TargetPos, RelativeTargetVel.normalized * GUN_TargetSpeedLerper);
                Vector3 PredictedPos = (TargetPos + (RelativeTargetVel.normalized * GUN_TargetSpeedLerper) * interceptTime);

                //Bulletdrop, technically incorrect implementation because it should be integrated into vintercept() but that'd be very difficult
                Vector3 gravity = new Vector3(0, -Physics.gravity.y * .5f * interceptTime * interceptTime, 0);//Bulletdrop
                // Vector3 TargetAccel = RelativeTargetVel - RelativeTargetVelLastFrame;
                // Vector3 accel = ((TargetAccel / deltaTime) * 0.5f * interceptTime * interceptTime); // accel causes jitter
                PredictedPos += gravity /* + accel */;

                GUNLeadIndicator.position = PredictedPos;
                //move lead indicator to match the distance of the rest of the hud
                GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;
                GUNLeadIndicator.rotation = Quaternion.LookRotation(GUNLeadIndicator.position - HudControlPosition, Rotator.transform.up);//This makes it not stretch when off to the side by fixing the rotation.

                RelativeTargetVelLastFrame = RelativeTargetVel;
            }
        }

        //not mine
        float vintercept(Vector3 fireorg, float missilespeed, Vector3 tgtorg, Vector3 tgtvel)
        {
            if (missilespeed <= 0)
                return (tgtorg - fireorg).magnitude / missilespeed;

            float tgtspd = tgtvel.magnitude;
            Vector3 dir = fireorg - tgtorg;
            float d = dir.magnitude;
            float a = missilespeed * missilespeed - tgtspd * tgtspd;
            float b = 2 * Vector3.Dot(dir, tgtvel);
            float c = -d * d;

            float t = 0;
            if (a == 0)
            {
                if (b == 0)
                    return 0f;
                else
                    t = -c / b;
            }
            else
            {
                float s0 = b * b - 4 * a * c;
                if (s0 <= 0)
                    return 0f;
                float s = Mathf.Sqrt(s0);
                float div = 1.0f / (2f * a);
                float t1 = -(s + b) * div;
                float t2 = (s - b) * div;
                if (t1 <= 0 && t2 <= 0)
                    return 0f;
                t = (t1 > 0 && t2 > 0) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
            }
            return t;
        }
    }
}