
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


//this script moves a seat to a position suitable for using a vehicle for any avatar, synced with other players, without the need for synced variables 

public class SaccSeatAdjuster : UdonSharpBehaviour
{
    public Transform Seat;
    public Transform TargetHeight;
    public bool CalibrateFowardBack = true;
    public Transform PositionTest;
    private Vector3 TargetRelative;
    private Vector3 SeatOriginalPos;
    private bool CalibratedY = false;
    private bool CalibratedZ = false;
    private VRCPlayerApi localPlayer;
    private float CalibrateTimer = 0f;
    private float AwakeTimer = 0f;
    private float scaleratio;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        SeatOriginalPos = Seat.transform.localPosition;
        gameObject.SetActive(false); //object needs to be active at least once to make the functions work over network
        scaleratio = transform.lossyScale.magnitude / Vector3.one.magnitude;
    }

    private void OnEnable()
    {
        //set seat back to it's original position
        if (localPlayer == null)
        {
            ResetSeat();
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetSeat");
        }

        CalibratedZ = false;
        CalibratedY = false;
        AwakeTimer = CalibrateTimer = 0f;
    }

    private void LateUpdate()
    {
        AwakeTimer += Time.deltaTime;
        CalibrateTimer += Time.deltaTime;
        if (CalibrateTimer > .3f)//do it about 3 times a second so we don't send too many broadcasts
        {
            if (localPlayer == null)//find test object relative position for editor testing
            {
                TargetRelative = TargetHeight.InverseTransformPoint(PositionTest.position);
            }
            else//find head relative position ingame
            {
                TargetRelative = TargetHeight.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
            }

            CalibrateY();
            if (CalibrateFowardBack == true) { CalibrateZ(); }
            else { CalibratedZ = true; }

            if (CalibratedY && CalibratedZ)//avatar 3.0 avatars have a weird delay when sitting so just wait a second to make sure we're calibrated
            {
                gameObject.SetActive(false);
            }
            CalibrateTimer = 0;
        }
    }



    public void CalibrateY()
    {
        if (CalibratedY == false)
        {
            //find out how far we are away and move towards it using binary search
            if (TargetRelative.y < -.64)
            {
                //Debug.Log("u64");
                if (localPlayer == null)//editor
                {
                    MoveUp64cm();
                }
                else//ingame
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp64cm");
                }
            }
            else if (TargetRelative.y < -.32)
            {
                //Debug.Log("u32");
                if (localPlayer == null)//editor
                {
                    MoveUp32cm();
                }
                else//ingame
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp32cm");
                }
            }
            else if (TargetRelative.y < -.16)
            {
                //Debug.Log("u16");
                if (localPlayer == null)
                {
                    MoveUp16cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp16cm");
                }
            }
            else if (TargetRelative.y < -.08)
            {
                //Debug.Log("u8");
                if (localPlayer == null)
                {
                    MoveUp8cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp8cm");
                }
            }
            else if (TargetRelative.y < -.04)
            {
                //Debug.Log("u4");
                if (localPlayer == null)
                {
                    MoveUp4cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp4cm");
                }
            }
            else if (TargetRelative.y < -.02)
            {
                //Debug.Log("u2");
                if (localPlayer == null)
                {
                    MoveUp2cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp2cm");
                }
            }
            else if (TargetRelative.y < -.01)
            {
                //Debug.Log("u1");
                if (localPlayer == null)
                {
                    MoveUp1cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveUp1cm");
                }
            }
            else if (TargetRelative.y > .64)
            {
                //Debug.Log("d64");
                if (localPlayer == null)
                {
                    MoveDown64cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown64cm");
                }
            }
            else if (TargetRelative.y > .32)
            {
                //Debug.Log("d32");
                if (localPlayer == null)
                {
                    MoveDown32cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown32cm");
                }
            }
            else if (TargetRelative.y > .16)
            {
                //Debug.Log("d16");
                if (localPlayer == null)
                {
                    MoveDown16cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown16cm");
                }
            }
            else if (TargetRelative.y > .08)
            {
                //Debug.Log("d8");
                if (localPlayer == null)
                {
                    MoveDown8cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown8cm");
                }
            }
            else if (TargetRelative.y > .04)
            {
                //Debug.Log("d4");
                if (localPlayer == null)
                {
                    MoveDown4cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown4cm");
                }
            }
            else if (TargetRelative.y > .02)
            {
                //Debug.Log("d2");
                if (localPlayer == null)
                {
                    MoveDown2cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown2cm");
                }
            }
            else if (TargetRelative.y > .01)
            {
                //Debug.Log("d1");
                if (localPlayer == null)
                {
                    MoveDown1cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveDown1cm");
                }
            }
            else if (AwakeTimer > 1)
            {
                CalibratedY = true;
            }
        }
    }

    public void CalibrateZ()
    {
        if (CalibratedZ == false)
        {
            //find out how far we are away and move towards it using binary search
            if (TargetRelative.z < -.64)
            {
                //Debug.Log("f64");
                if (localPlayer == null)//editor
                {
                    MoveForward64cm();
                }
                else//ingame
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward64cm");
                }
            }
            else if (TargetRelative.z < -.32)
            {
                //Debug.Log("f32");
                if (localPlayer == null)//editor
                {
                    MoveForward32cm();
                }
                else//ingame
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward32cm");
                }
            }
            else if (TargetRelative.z < -.16)
            {
                //Debug.Log("f16");
                if (localPlayer == null)
                {
                    MoveForward16cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward16cm");
                }
            }
            else if (TargetRelative.z < -.08)
            {
                //Debug.Log("f8");
                if (localPlayer == null)
                {
                    MoveForward8cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward8cm");
                }
            }
            else if (TargetRelative.z < -.04)
            {
                //Debug.Log("f4");
                if (localPlayer == null)
                {
                    MoveForward4cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward4cm");
                }
            }
            else if (TargetRelative.z < -.02)
            {
                //Debug.Log("f2");
                if (localPlayer == null)
                {
                    MoveForward2cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward2cm");
                }
            }
            else if (TargetRelative.z < -.01)
            {
                //Debug.Log("f1");
                if (localPlayer == null)
                {
                    MoveForward1cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveForward1cm");
                }
            }
            else if (TargetRelative.z > .64)
            {
                //Debug.Log("b64");
                if (localPlayer == null)
                {
                    MoveBack64cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack64cm");
                }
            }
            else if (TargetRelative.z > .32)
            {
                //Debug.Log("b32");
                if (localPlayer == null)
                {
                    MoveBack32cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack32cm");
                }
            }
            else if (TargetRelative.z > .16)
            {
                //Debug.Log("b16");
                if (localPlayer == null)
                {
                    MoveBack16cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack16cm");
                }
            }
            else if (TargetRelative.z > .08)
            {
                //Debug.Log("b8");
                if (localPlayer == null)
                {
                    MoveBack8cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack8cm");
                }
            }
            else if (TargetRelative.z > .04)
            {
                //Debug.Log("b4");
                if (localPlayer == null)
                {
                    MoveBack4cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack4cm");
                }
            }
            else if (TargetRelative.z > .02)
            {
                //Debug.Log("b2");
                if (localPlayer == null)
                {
                    MoveBack2cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack2cm");
                }
            }
            else if (TargetRelative.z > .01)
            {
                //Debug.Log("b1");
                if (localPlayer == null)
                {
                    MoveBack1cm();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MoveBack1cm");
                }
            }
            else if (AwakeTimer > 1)
            {
                CalibratedZ = true;
            }
        }
    }




    public void ResetSeat()
    {
        Seat.localPosition = SeatOriginalPos;
    }


    public void MoveUp1cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .01f * scaleratio);
    }
    public void MoveDown1cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.01f * scaleratio);
    }

    public void MoveUp2cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .02f * scaleratio);
    }
    public void MoveDown2cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.02f * scaleratio);
    }



    public void MoveUp4cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .04f * scaleratio);
    }
    public void MoveDown4cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.04f * scaleratio);
    }



    public void MoveUp8cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .08f * scaleratio);
    }
    public void MoveDown8cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.08f * scaleratio);
    }




    public void MoveUp16cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .16f * scaleratio);
    }
    public void MoveDown16cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.16f * scaleratio);
    }


    public void MoveUp32cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .32f * scaleratio);
    }
    public void MoveDown32cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.32f * scaleratio);
    }


    public void MoveUp64cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * .64f * scaleratio);
    }
    public void MoveDown64cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.up * -.64f * scaleratio);
    }












    public void MoveForward1cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .01f * scaleratio);
    }
    public void MoveBack1cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.01f * scaleratio);
    }

    public void MoveForward2cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .02f * scaleratio);
    }
    public void MoveBack2cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.02f * scaleratio);
    }



    public void MoveForward4cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .04f * scaleratio);
    }
    public void MoveBack4cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.04f * scaleratio);
    }



    public void MoveForward8cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .08f * scaleratio);
    }
    public void MoveBack8cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.08f * scaleratio);
    }




    public void MoveForward16cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .16f * scaleratio);
    }
    public void MoveBack16cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.16f * scaleratio);
    }


    public void MoveForward32cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .32f * scaleratio);
    }
    public void MoveBack32cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.32f * scaleratio);
    }


    public void MoveForward64cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * .64f * scaleratio);
    }
    public void MoveBack64cm()
    {
        Seat.position += Seat.TransformDirection(Vector3.forward * -.64f * scaleratio);
    }





}
