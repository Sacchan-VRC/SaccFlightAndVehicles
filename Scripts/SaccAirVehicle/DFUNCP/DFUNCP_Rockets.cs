
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNCP_Rockets : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public GameObject Rocket;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        public int NumRocket = 4;
        [Tooltip("How often rocket fires if the trigger is held down")]
        public float RocketHoldDelay = 0.5f;
        [Tooltip("Minimum time between firing rockets")]
        public float RocketDelay = 0f;
        public Transform[] RocketLaunchPoints;
        [Tooltip("Transform of which its X scale scales with ammo")]
        public Transform AmmoBar;
        public KeyCode LaunchRocketKey = KeyCode.C;
        [Tooltip("Fired projectiles will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private float Trigger;
        private bool TriggerLastFrame = true;
        private int RocketPoint = 0;
        private float LastRocketDropTime = 0f;
        private int FullRockets;
        private float FullRocketsDivider;
        private Transform VehicleTransform;
        private float reloadspeed;
        private Vector3 AmmoBarScaleStart;
        private int NumChildrenStart;
        private VRCPlayerApi localPlayer;
        private bool InVR;
        private bool IsOwner;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXTP_L_EntityStart()
        {
            FullRockets = NumRocket;
            reloadspeed = FullRockets / FullReloadTimeSec;
            FullRocketsDivider = 1f / (NumRocket > 0 ? NumRocket : 10000000);
            if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; AmmoBar.gameObject.SetActive(false); }
            if (RocketHoldDelay < RocketDelay) { RocketHoldDelay = RocketDelay; }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleTransform = EntityControl.transform;

            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                InVR = localPlayer.IsUserInVR();
            }

            NumChildrenStart = transform.childCount;
            if (Rocket)
            {
                int NumToInstantiate = Mathf.Min(FullRockets, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Object.Instantiate(Rocket);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        public void SFEXTP_O_UserEnter()
        {
            if (AmmoBar) { AmmoBar.gameObject.SetActive(true); }
            if (!InVR)
            {
                DFUNC_Selected();
            }
        }
        public void SFEXTP_O_UserExit()
        {
            if (AmmoBar) { AmmoBar.gameObject.SetActive(false); }
            DFUNC_Deselected();
        }
        public void SFEXTP_G_Explode()
        {
            RocketPoint = 0;
            NumRocket = FullRockets;
            UpdateAmmoVisuals();
        }
        public void SFEXTP_G_RespawnButton()
        {
            NumRocket = FullRockets;
            RocketPoint = 0;
            UpdateAmmoVisuals();
        }
        public void SFEXTP_G_ReSupply()
        {
            if (NumRocket != FullRockets) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            NumRocket = (int)Mathf.Min(NumRocket + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullRockets);
            RocketPoint = 0;
            UpdateAmmoVisuals();
        }
        public void UpdateAmmoVisuals()
        {
            if (AmmoBar) { AmmoBar.localScale = new Vector3((NumRocket * FullRocketsDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
        }
        private void Update()
        {
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (Trigger > 0.75 || (Input.GetKey(LaunchRocketKey)))
            {
                if (!TriggerLastFrame)
                {
                    if (NumRocket > 0 && ((Time.time - LastRocketDropTime) > RocketDelay))
                    {
                        LastRocketDropTime = Time.time;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchRocket");
                    }
                }
                else//launch every RocketHoldDelay
                    if (NumRocket > 0 && ((Time.time - LastRocketDropTime) > RocketHoldDelay))
                {
                    {
                        LastRocketDropTime = Time.time;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchRocket");
                    }
                }

                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void LaunchRocket()
        {
            IsOwner = localPlayer.IsOwner(gameObject);
            if (NumRocket > 0) { NumRocket--; }
            UpdateAmmoVisuals();
            if (Rocket != null)
            {
                GameObject NewRocket;
                if (transform.childCount - NumChildrenStart > 0)
                { NewRocket = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { NewRocket = InstantiateWeapon(); }
                if (WorldParent) { NewRocket.transform.SetParent(WorldParent); }
                else { NewRocket.transform.SetParent(null); }
                NewRocket.transform.SetPositionAndRotation(RocketLaunchPoints[RocketPoint].position, RocketLaunchPoints[RocketPoint].rotation);
                NewRocket.SetActive(true);
                NewRocket.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                RocketPoint++;
                if (RocketPoint == RocketLaunchPoints.Length) RocketPoint = 0;
            }
        }
    }
}