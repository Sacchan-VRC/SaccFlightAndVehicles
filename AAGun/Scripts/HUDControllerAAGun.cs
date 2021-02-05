
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class HUDControllerAAGun : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public Transform ElevationIndicator;
    public Transform HeadingIndicator;
    public Transform AAMTargetIndicator;
    public Transform GUNLeadIndicator;
    public Transform AAMReloadBar;
    public Transform MGReloadBar;
    public Text HUDText_AAM_ammo;
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
    private void Start()
    {
        Assert(AAGunControl != null, "Start: AAGunControl != null");
        Assert(ElevationIndicator != null, "Start: ElevationIndicator != null");
        Assert(HeadingIndicator != null, "Start: HeadingIndicator != null");
        BulletSpeedDivider = 1f / BulletSpeed;
        AAMReloadBarDivider = 1f / AAGunControl.MissileReloadTime;
        MGReloadBarDivider = 1f / AAGunControl.MGAmmoFull;

        Rotator = AAGunControl.Rotator.transform;
    }
    private void Update()
    {
        float DeltaTime = Time.deltaTime;
        //AAM Target Indicator
        if (AAGunControl.AAMHasTarget)
        {
            AAMTargetIndicator.localScale = new Vector3(1, 1, 1);
            AAMTargetIndicator.position = transform.position + AAGunControl.AAMCurrentTargetDirection;
            AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
            if (AAGunControl.AAMLocked)
            {
                AAMTargetIndicator.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));//back of mesh is locked version
            }
            else
            {
                AAMTargetIndicator.localRotation = Quaternion.identity;
            }
        }
        else AAMTargetIndicator.localScale = Vector3.zero;
        /////////////////

        //AAMs
        HUDText_AAM_ammo.text = AAGunControl.NumAAM.ToString("F0");
        /////////////////

        //AAM Reload bar
        AAMReloadBar.localScale = new Vector3(AAGunControl.AAMReloadTimer * AAMReloadBarDivider, .3f, 0);
        /////////////////

        //MG Reload bar
        MGReloadBar.localScale = new Vector3(AAGunControl.MGAmmoSeconds * MGReloadBarDivider, .3f, 0);
        /////////////////

        //GUN Lead Indicator
        if (AAGunControl.AAMHasTarget)
        {
            GUNLeadIndicator.gameObject.SetActive(true);
            Vector3 TargetDir;
            if (AAGunControl.AAMCurrentTargetEngineControl == null)//target is a dummy target
            { TargetDir = AAGunControl.AAMTargets[AAGunControl.AAMTarget].transform.position - transform.position; }
            else
            { TargetDir = AAGunControl.AAMCurrentTargetEngineControl.CenterOfMass.position - transform.position; }

            Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;
            Vector3 TargetAccel = RelativeTargetVel - RelativeTargetVelLastFrame;
            //GUN_TargetDirOld is around 10 frames worth of distance behind a moving target (lerped by .1) in order to smooth out the calculation for unsmooth netcode
            //multiplying the result by .1(to get back to 1 frames worth) seems to actually give an accurate enough result to use in prediction
            GUN_TargetSpeedLerper = Mathf.Lerp(GUN_TargetSpeedLerper, (RelativeTargetVel.magnitude * .1f) / DeltaTime, .6f * DeltaTime);
            float BulletHitTime = TargetDir.magnitude / BulletSpeed;
            //normalize lerped relative target velocity vector and multiply by lerped speed
            Vector3 RelTargVelNormalized = RelativeTargetVel.normalized;
            Vector3 PredictedPos = TargetDir
                + (((RelTargVelNormalized * GUN_TargetSpeedLerper)//Linear
                    + (TargetAccel * .05f * BulletHitTime)//Acceleration
                        + new Vector3(0, 9.81f * .5f * BulletHitTime, 0))//Bulletdrop
                            * BulletHitTime);
            GUNLeadIndicator.position = transform.position + PredictedPos;
            GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;

            RelativeTargetVelLastFrame = RelativeTargetVel;
            GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, .1f);
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
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}