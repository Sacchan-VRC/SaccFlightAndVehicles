
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAAG_HUDController : UdonSharpBehaviour
{
    [Tooltip("Transform of the pilot seat's target eye position, HUDContrller is automatically moved to this position in Start() to ensure perfect alignment")]
    [SerializeField] private Transform PilotSeatAdjusterTarget;
    public SaccAAGunController AAGunControl;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    public Transform AAMTargetIndicator;
    public Transform GUNLeadIndicator;
    public Transform AAMReloadBar;
    public Transform MGAmmoBar;
    public Text HUDText_AAM_ammo;
    [Tooltip("Local distance projected forward for objects that move dynamically, only adjust if the hud is moved forward in order to make it appear smaller")]
    public float distance_from_head = 1.333f;
    [System.NonSerializedAttribute] public Vector3 GUN_TargetDirOld;
    [System.NonSerializedAttribute] public float GUN_TargetSpeedLerper;
    [System.NonSerializedAttribute] public Vector3 RelativeTargetVel;
    [System.NonSerializedAttribute] public Vector3 RelativeTargetVelLastFrame;
    public float BulletSpeed = 1050;
    private float BulletSpeedDivider;
    private float AAMReloadBarDivider;
    private float MGReloadBarDivider;
    private Transform Rotator;
    private Quaternion backfacing = Quaternion.Euler(new Vector3(0, 180, 0));
    private Quaternion frontfacing = Quaternion.identity;
    private void Start()
    {
        BulletSpeedDivider = 1f / BulletSpeed;
        AAMReloadBarDivider = 1f / AAGunControl.MissileReloadTime;
        MGReloadBarDivider = 1f / AAGunControl.MGAmmoSeconds;
        if (PilotSeatAdjusterTarget) { transform.position = PilotSeatAdjusterTarget.position; }

        Rotator = AAGunControl.Rotator.transform;
    }
    private void Update()
    {
        float SmoothDeltaTime = Time.smoothDeltaTime;
        //AAM Target Indicator
        if (AAGunControl.AAMHasTarget)
        {
            AAMTargetIndicator.gameObject.SetActive(true);
            AAMTargetIndicator.position = transform.position + AAGunControl.AAMCurrentTargetDirection;
            AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
            AAMTargetIndicator.localRotation = frontfacing;
            if (AAGunControl.AAMLocked)
            {
                AAMTargetIndicator.localRotation = backfacing;//back of mesh is locked version
            }
        }
        else
        {
            AAMTargetIndicator.gameObject.SetActive(false);
        }
        /////////////////

        //AAMs
        HUDText_AAM_ammo.text = AAGunControl.NumAAM.ToString("F0");
        /////////////////

        //AAM Reload bar
        AAMReloadBar.localScale = new Vector3(AAGunControl.AAMReloadTimer * AAMReloadBarDivider, .3f, 0);
        /////////////////

        //MG Reload bar
        MGAmmoBar.localScale = new Vector3(AAGunControl.MGAmmoSeconds * MGReloadBarDivider, .3f, 0);
        /////////////////

        //GUN Lead Indicator
        if (AAGunControl.AAMHasTarget)
        {
            GUNLeadIndicator.gameObject.SetActive(true);
            Vector3 TargetDir;
            if (!AAGunControl.AAMCurrentTargetSAVControl)//target is a dummy target
            { TargetDir = AAGunControl.AAMTargets[AAGunControl.AAMTarget].transform.position - transform.position; }
            else
            { TargetDir = AAGunControl.AAMCurrentTargetSAVControl.CenterOfMass.position - transform.position; }
            GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, .2f);

            Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;
            Vector3 TargetAccel = RelativeTargetVel - RelativeTargetVelLastFrame;
            //GUN_TargetDirOld is around 4 frames worth of distance behind a moving target (lerped by .2) in order to smooth out the calculation for unsmooth netcode
            //multiplying the result by .25(to get back to 1 frames worth) seems to actually give an accurate enough result to use in prediction
            GUN_TargetSpeedLerper = Mathf.Lerp(GUN_TargetSpeedLerper, (RelativeTargetVel.magnitude * .25f) / SmoothDeltaTime, 15 * SmoothDeltaTime);
            float BulletHitTime = TargetDir.magnitude / BulletSpeed;
            //normalize lerped relative target velocity vector and multiply by lerped speed
            Vector3 RelTargVelNormalized = RelativeTargetVel.normalized;
            Vector3 PredictedPos = TargetDir
                + (((RelTargVelNormalized * GUN_TargetSpeedLerper)/* Linear */
                    //the .125 in the next line is combined .25 for undoing the lerp, and .5 for the acceleration formula
                    + (-TargetAccel * .125f * BulletHitTime)//Acceleration
                        + new Vector3(0, 9.81f * .5f * BulletHitTime, 0))//Bulletdrop
                            * BulletHitTime);
            GUNLeadIndicator.position = transform.position + PredictedPos;
            GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;

            RelativeTargetVelLastFrame = RelativeTargetVel;
        }
        else GUNLeadIndicator.gameObject.SetActive(false);
        /////////////////

        //Heading indicator
        Vector3 newrot = new Vector3(0, Rotator.rotation.eulerAngles.y, 0);
        HeadingIndicator.localRotation = Quaternion.Euler(-newrot);
        /////////////////

        //Elevation indicator
        ElevationIndicator.rotation = Quaternion.Euler(newrot);
        /////////////////
    }
}