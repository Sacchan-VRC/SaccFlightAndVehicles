
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindChangerRespawner : UdonSharpBehaviour
{
    public GameObject WindChangerObj;
    public Transform RespawnPoint;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        Assert(WindChangerObj != null, "Start: WindChangerObj != null");
        Assert(RespawnPoint != null, "Start: RespawnPoint != null");

        localPlayer = Networking.LocalPlayer;
    }
    void Interact()
    {
        Networking.SetOwner(localPlayer, WindChangerObj);
        WindChangerObj.transform.position = RespawnPoint.position;
        WindChangerObj.transform.rotation = RespawnPoint.rotation;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
