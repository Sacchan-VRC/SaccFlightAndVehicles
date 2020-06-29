
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunController : UdonSharpBehaviour
{
    public GameObject Rotator;
    public GameObject VehicleMainObj;
    private Animator AAGunAnimator;
    public VRCStation AAGunSeatStation;
    public float TurnSpeedMulti = 1;
    public float TurningResponse = 1f;
    public float StopSpeed = 4.5f;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    private float LstickH;
    private float LstickV;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool firing;
    private float LTrigger = 0;
    private float FullHealth;
    private Vector3 Rotinputlerper;
    [System.NonSerializedAttribute] [HideInInspector] public bool Manning; //like Piloting in the plane
    [System.NonSerializedAttribute] [HideInInspector] public VRCPlayerApi localPlayer;
    private float InputXLerper = 0f;
    private float InputYLerper = 0f;
    void Start()
    {
        if (VehicleMainObj != null) { AAGunAnimator = VehicleMainObj.GetComponent<Animator>(); }
        FullHealth = Health;
        localPlayer = Networking.LocalPlayer;
    }
    void Update()
    {
        if (localPlayer == null || localPlayer.IsOwner(VehicleMainObj))
        {
            if (Health < 0)
            {
                if (localPlayer == null)
                {
                    Explode();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }
            if (localPlayer == null || Manning)
            {
                //get inputs
                float Wf = 0; //inputs as floats
                float Af = 0;
                float Sf = 0;
                float Df = 0;
                if (Input.GetKey(KeyCode.W)) { Wf = 1; }
                if (Input.GetKey(KeyCode.A)) { Af = -1; }
                if (Input.GetKey(KeyCode.S)) { Sf = -1; }
                if (Input.GetKey(KeyCode.D)) { Df = 1; }
                LstickH = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LstickV = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                //lerp to inputs for smooth motion
                float InputY = Mathf.Clamp((LstickH + Af + Df), -1, 1) * TurnSpeedMulti;
                float InputX = Mathf.Clamp((LstickV + Wf + Sf), -1, 1) * TurnSpeedMulti;

                if (InputY > 0 && InputYLerper < 0 || InputY < 0 && InputYLerper > 0 || Mathf.Abs(InputYLerper) > Mathf.Abs(InputY))
                {
                    InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * Time.deltaTime);
                    InputYLerper *= StopSpeed;
                }
                else
                {
                    InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * Time.deltaTime);
                }

                if (InputX > 0 && InputXLerper < 0 || InputX < 0 && InputXLerper > 0 || Mathf.Abs(InputXLerper) > Mathf.Abs(InputX))
                {
                    InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * Time.deltaTime);
                    InputXLerper *= StopSpeed;
                }
                else
                {
                    InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * Time.deltaTime);
                }


                float temprot = Rotator.transform.localRotation.eulerAngles.x;
                if (temprot > 180) { temprot -= 360; }
                temprot += InputXLerper;
                temprot = Mathf.Clamp(temprot, -90, 35);
                Rotator.transform.localRotation = Quaternion.Euler(new Vector3(temprot, Rotator.transform.localRotation.eulerAngles.y + InputYLerper, 0));

                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //Firing the gun
                if (LTrigger >= 0.75 || Input.GetKey(KeyCode.Space))
                {
                    firing = true;
                }
                else
                {
                    firing = false;
                }
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
        AAGunAnimator.SetFloat("health", Health);
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        if (Manning)
        {
            if (AAGunSeatStation != null) { AAGunSeatStation.ExitStation(localPlayer); }
        }
        AAGunAnimator.SetTrigger("explode");
        Health = FullHealth;//turns off low health smoke
    }
}
