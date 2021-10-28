
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNCP_Turret : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private Animator TurretAnimator;
    [SerializeField] private Transform TurretRotatorHor;
    [SerializeField] private Transform TurretRotatorVert;
    [SerializeField] private float TurnSpeedMulti = 6;
    [Tooltip("Rotation slowdown per frame")]
    [Range(0, 1)]
    [SerializeField] private float TurnFriction = .04f;
    [Tooltip("Angle above the horizon that this gun can look")]
    [SerializeField] private float UpAngleMax = 89;
    [Tooltip("Angle below the horizon that this gun can look")]
    [SerializeField] private float DownAngleMax = 0;
    [Tooltip("Angle left that this gun can look, set both to 180 to freely spin")]
    [SerializeField] private float LeftAngleMax = 180;
    [Tooltip("Angle right that this gun can look, set both to 180 to freely spin")]
    [SerializeField] private float RightAngleMax = 180;
    [Tooltip("In seconds")]
    [Range(0.05f, 1f)]
    [SerializeField] private float updateInterval = 0.25f;
    [SerializeField] private GameObject Projectile;
    [SerializeField] private AudioSource FireSound;
    [SerializeField] private Transform AmmoBar;
    [SerializeField] private int Ammo = 60;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 8;
    [Tooltip("Minimum delay between firing")]
    [SerializeField] private float FireDelay = 0f;
    [Tooltip("Delay between firing when holding the trigger")]
    [SerializeField] private float FireHoldDelay = 0.5f;
    [SerializeField] private Transform[] FirePoints;
    private float LastFireTime = 0f;
    private int FullAmmo;
    private float FullAmmoDivider;
    private float StartHorTurnSpeed;
    private float StartVertTurnSpeed;
    private float InputXKeyb;
    private float InputYKeyb;
    private float RotationSpeedX = 0f;
    private float RotationSpeedY = 0f;
    private Vector3 AmmoBarScaleStart;
    private float reloadspeed;
    private bool InEditor = true;
    private bool InVR;
    private VRCPlayerApi localPlayer;

    private int StartupTimeMS = 0;
    private int O_LastUpdateTime;
    private int L_UpdateTime;
    private int L_LastUpdateTime;
    private float LastPing;
    private float Ping;
    private float nextUpdateTime = 0;
    private double StartupTime;
    private Vector2 LastGunRotationSpeed;
    private Vector2 GunRotationSpeed;
    private Vector2 O_LastGunRotation2;
    private Vector2 O_LastGunRotation;
    private int O_LastUpdateTime2;
    private float SmoothingTimeDivider;
    private bool Occupied;
    private bool TriggerLastFrame;
    private bool Manning;
    [System.NonSerializedAttribute] public bool IsOwner;//required by the bomb script, not actually related to being the owner of the object
    [UdonSynced(UdonSyncMode.None)] private Vector2 O_GunRotation;
    [UdonSynced(UdonSyncMode.None)] private int O_UpdateTime = 0;
    public void SFEXTP_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        InEditor = localPlayer == null;

        nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
        SmoothingTimeDivider = 1f / updateInterval;
        StartupTimeMS = Networking.GetServerTimeInMilliseconds();
        FullAmmoDivider = 1f / (Ammo > 0 ? Ammo : 10000000);
        if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; }
        reloadspeed = FullAmmo / FullReloadTimeSec;
    }
    public void SFEXTP_O_UserEnter()
    {
        IsOwner = true;
        Manning = true;
        if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Active));
    }
    public void SFEXTP_O_UserExit()
    {
        IsOwner = false;
        Manning = false;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_NotActive));
    }
    public void Set_Active()
    {
        gameObject.SetActive(true);
        if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
    }
    public void Set_NotActive() { gameObject.SetActive(false); }
    public void FireCannon()
    {
        int fp = FirePoints.Length;
        if (Ammo > 0) { Ammo--; }
        for (int x = 0; x < fp; x++)
        {
            GameObject proj = Object.Instantiate(Projectile);
            proj.transform.SetPositionAndRotation(FirePoints[x].position, FirePoints[x].rotation);
            proj.SetActive(true);
            proj.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
        }
        FireSound.PlayOneShot(FireSound.clip);
        if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
    }
    public void SFEXTP_G_ReSupply()
    {
        if (Ammo != FullAmmo) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
        Ammo = (int)Mathf.Min(Ammo + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAmmo);
        if (AmmoBar) { AmmoBar.localScale = new Vector3((Ammo * FullAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
    }
    private void Update()
    {
        if (Manning)
        {
            //GUN
            float Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
            {
                if (!TriggerLastFrame)
                {
                    if (Ammo > 0 && ((Time.time - LastFireTime) > FireDelay))
                    {
                        LastFireTime = Time.time;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireCannon));
                    }
                }
                else if (Ammo > 0 && ((Time.time - LastFireTime) > FireHoldDelay))
                {//launch every FireHoldDelay
                    LastFireTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireCannon));
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }



            //ROTATION
            float HorDif = 0;
            float VertDif = 0;
            float HorDot = 1;
            if (InVR)
            {
                if (Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger") > .75)
                {
                    Vector3 RHandDir = (localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0)) * Vector3.forward;
                    HorDif = Vector3.SignedAngle(TurretRotatorHor.forward, Vector3.ProjectOnPlane(RHandDir, TurretRotatorHor.up), TurretRotatorHor.up);
                    HorDot = Mathf.Abs(Vector3.Dot(TurretRotatorHor.forward, RHandDir));
                    VertDif = Vector3.SignedAngle(TurretRotatorVert.forward, Vector3.ProjectOnPlane(RHandDir, TurretRotatorHor.right), TurretRotatorHor.right);
                }
            }
            else
            {
                Vector3 HeadDir = (localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation) * Vector3.forward;
                HorDif = Vector3.SignedAngle(TurretRotatorHor.forward, Vector3.ProjectOnPlane(HeadDir, TurretRotatorHor.up), TurretRotatorHor.up);
                HorDot = Mathf.Abs(Vector3.Dot(TurretRotatorHor.forward, HeadDir));
                VertDif = Vector3.SignedAngle(TurretRotatorVert.forward, Vector3.ProjectOnPlane(HeadDir, TurretRotatorHor.right), TurretRotatorHor.right);
            }

            if (Mathf.Abs(HorDif) < 1.5f) { HorDif = 0; }
            HorDif *= .02f;
            if (Mathf.Abs(VertDif) < 1.5f) { VertDif = 0; }
            VertDif *= .02f * HorDot;

            Vector2 VRPitchYawInput = new Vector2(VertDif, HorDif);
            float DeltaTime = Time.smoothDeltaTime;

            float InputX = Mathf.Clamp((VRPitchYawInput.x), -1, 1);
            float InputY = Mathf.Clamp((VRPitchYawInput.y), -1, 1);
            //joystick model movement

            InputX *= TurnSpeedMulti;
            InputY *= TurnSpeedMulti;

            RotationSpeedX += -(RotationSpeedX * TurnFriction) + (InputX);
            RotationSpeedY += -(RotationSpeedY * TurnFriction) + (InputY);

            //rotate turret
            Vector3 rothor = TurretRotatorHor.localRotation.eulerAngles;
            Vector3 rotvert = TurretRotatorVert.localRotation.eulerAngles;

            float NewX = rotvert.x;
            NewX += RotationSpeedX * DeltaTime;
            if (NewX > 180) { NewX -= 360; }
            if (NewX > DownAngleMax || NewX < -UpAngleMax) RotationSpeedX = 0;
            NewX = Mathf.Clamp(NewX, -UpAngleMax, DownAngleMax);//limit angles

            float NewY = rothor.y;
            NewY += RotationSpeedY * DeltaTime;
            if (NewY > 180) { NewY -= 360; }
            if (NewY > RightAngleMax || NewY < -LeftAngleMax) RotationSpeedY = 0;
            NewY = Mathf.Clamp(NewY, -LeftAngleMax, RightAngleMax);//limit angles

            TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, NewY, 0));
            TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(NewX, 0, 0));

            if (Time.time > nextUpdateTime)
            {
                O_UpdateTime = Networking.GetServerTimeInMilliseconds();
                O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.localEulerAngles.y);
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        else
        {
            float TimeSinceUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_UpdateTime) * .001f);
            Vector2 PredictedRotation = O_GunRotation + (GunRotationSpeed * (Ping + TimeSinceUpdate));
            PredictedRotation.x = Mathf.Clamp(PredictedRotation.x, -UpAngleMax, DownAngleMax);

            Vector3 PredictedRotation_3 = new Vector3(PredictedRotation.x, PredictedRotation.y, 0);

            if (TimeSinceUpdate < updateInterval)
            {
                float TimeSincePreviousUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_LastUpdateTime) * .001f);

                Vector2 OldPredictedRotation = O_LastGunRotation2 + (LastGunRotationSpeed * (LastPing + TimeSincePreviousUpdate));
                OldPredictedRotation.x = Mathf.Clamp(OldPredictedRotation.x, -UpAngleMax, DownAngleMax);

                Vector3 OldPredictedRotation_3 = new Vector3(OldPredictedRotation.x, OldPredictedRotation.y, 0);

                Vector3 TargetRot = Vector3.Lerp(OldPredictedRotation_3, PredictedRotation_3, TimeSinceUpdate * SmoothingTimeDivider);
                TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, TargetRot.y, 0));
                TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(TargetRot.x, 0, 0));
            }
            else
            {
                TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, PredictedRotation_3.y, 0));
                TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(PredictedRotation_3.x, 0, 0));
            }
        }
    }
    public void SFEXTP_O_PlayerJoined()
    {
        if (localPlayer.IsOwner(gameObject) && Manning)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Active));
        }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {//when we take ownership because someone timed out, we don't want it left active
        if (player.isLocal && gameObject.activeSelf && !Manning)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_NotActive));
        }
    }
    public override void OnDeserialization()
    {
        if (O_UpdateTime != O_LastUpdateTime)//only do anything if OnDeserialization was for this script
        {
            if (O_GunRotation.x > 180) { O_GunRotation.x -= 360; }
            LastPing = Ping;
            L_LastUpdateTime = L_UpdateTime;
            float updatedelta = (O_UpdateTime - O_LastUpdateTime) * .001f;
            float speednormalizer = 1 / updatedelta;

            L_UpdateTime = Networking.GetServerTimeInMilliseconds();
            Ping = (L_UpdateTime - O_UpdateTime) * .001f;
            LastGunRotationSpeed = GunRotationSpeed;

            //check if going from rotation 0->360 and fix values for interpolation
            if (Mathf.Abs(O_GunRotation.y - O_LastGunRotation.y) > 180)
            {
                if (O_GunRotation.y > O_LastGunRotation.y)
                {
                    O_LastGunRotation.y += 360;
                }
                else
                {
                    O_LastGunRotation.y -= 360;
                }
            }
            GunRotationSpeed = (O_GunRotation - O_LastGunRotation) * speednormalizer;
            O_LastGunRotation2 = O_LastGunRotation;
            O_LastGunRotation = O_GunRotation;
            O_LastUpdateTime = O_UpdateTime;
        }
    }
}
