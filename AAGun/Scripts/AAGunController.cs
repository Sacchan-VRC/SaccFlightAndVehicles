
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunController : UdonSharpBehaviour
{
    public GameObject Rotator;
    public GameObject VehicleMainObj;
    public VRCStation AAGunSeatStation;
    public Camera AACam;
    public float TurnSpeedMulti = 10;
    public float TurningResponse = .2f;
    public float StopSpeed = .95f;
    public float ZoomFov = .1f;
    public float ZoomOutFov = 110f;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    private Animator AAGunAnimator;
    [System.NonSerializedAttribute] public bool dead;

    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool firing;
    private float RTrigger = 0;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Manning;//like Piloting in the plane
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public float InputXLerper = 0f;
    [System.NonSerializedAttribute] public float InputYLerper = 0f;
    private Vector3 StartRot;
    private float RstickV;
    private float ZoomLevel;
    private bool InEditor = true;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null) { InEditor = false; }

        Assert(Rotator != null, "Start: Rotator != null");
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(AAGunSeatStation != null, "Start: AAGunSeatStation != null");
        Assert(AACam != null, "Start: AACam != null");

        if (VehicleMainObj != null) { AAGunAnimator = VehicleMainObj.GetComponent<Animator>(); }
        FullHealth = Health;
        StartRot = Rotator.transform.localRotation.eulerAngles;
        if (StopSpeed > 1) StopSpeed = .999f;
    }
    void Update()
    {
        if (InEditor || localPlayer.IsOwner(VehicleMainObj))
        {
            if (Health <= 0)
            {
                if (InEditor)
                {
                    Explode();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }
            if (InEditor || Manning)
            {
                //get inputs
                float Wf = Input.GetKey(KeyCode.W) ? -1 : 0; //inputs as floats
                float Sf = Input.GetKey(KeyCode.S) ? 1 : 0;
                float Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                float Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                float RstickH = 0;
                float LstickV = 0;
                if (!InEditor)
                {
                    RstickH = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                    RstickV = -Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                    LstickV = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                }

                //lerp to inputs for smooth motion
                float InputY = Mathf.Clamp((RstickH + Af + Df), -1, 1) * TurnSpeedMulti;
                float InputX = Mathf.Clamp((RstickV + Wf + Sf), -1, 1) * TurnSpeedMulti;


                //Camera control
                if (AACam != null) { ZoomLevel = AACam.fieldOfView / 90; }
                if (Mathf.Abs(LstickV) > .1)
                {
                    if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - 3.2f * LstickV * ZoomLevel, ZoomFov, ZoomOutFov); }
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - 1.6f * ZoomLevel, ZoomFov, ZoomOutFov); }
                }
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView + 1.6f * ZoomLevel, ZoomFov, ZoomOutFov); }
                }



                //only do friction if slowing down or trying to turn in the oposite direction
                if (InputY > 0 && InputYLerper < 0 || InputY < 0 && InputYLerper > 0 || Mathf.Abs(InputYLerper) > Mathf.Abs(InputY))
                {
                    InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * Time.deltaTime);
                    InputYLerper *= StopSpeed;
                }
                else { InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * Time.deltaTime); }

                if (InputX > 0 && InputXLerper < 0 || InputX < 0 && InputXLerper > 0 || Mathf.Abs(InputXLerper) > Mathf.Abs(InputX))
                {
                    InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * Time.deltaTime);
                    InputXLerper *= StopSpeed;
                }
                else { InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * Time.deltaTime); }


                float temprot = Rotator.transform.localRotation.eulerAngles.x;
                temprot += InputXLerper * ZoomLevel;
                if (temprot > 180) { temprot -= 360; }
                temprot = Mathf.Clamp(temprot, -89, 35);
                Rotator.transform.localRotation = Quaternion.Euler(new Vector3(temprot, Rotator.transform.localRotation.eulerAngles.y + (InputYLerper * ZoomLevel), 0));

                //Firing the gun
                if (RTrigger >= 0.75 || Input.GetKey(KeyCode.Space))
                {
                    firing = true;
                }
                else { firing = false; }
            }
            else
            {
                firing = false;
            }
        }
        if (firing)
        {
            AAGunAnimator.SetBool("firing", true);
        }
        else
        {
            AAGunAnimator.SetBool("firing", false);
        }
        if (!dead)
        {
            AAGunAnimator.SetFloat("health", Health / FullHealth);
        }
        else
        {
            AAGunAnimator.SetFloat("health", 1);//dead, set animator health to full so that there's no phantom healthsmoke
        }

    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        if (Manning)
        {
            if (AAGunSeatStation != null) { AAGunSeatStation.ExitStation(localPlayer); }
        }
        dead = true;
        AAGunAnimator.SetTrigger("explode");
        Health = FullHealth;//turns off low health smoke
        if (localPlayer.IsOwner(VehicleMainObj))
        {
            Rotator.transform.localRotation = Quaternion.Euler(StartRot);
        }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
