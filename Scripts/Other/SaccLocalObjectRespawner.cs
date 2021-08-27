
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccLocalObjectRespawner : UdonSharpBehaviour
{
    public GameObject WindChangerObj;
    public Transform RespawnPoint;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }
    void Interact()
    {
        Networking.SetOwner(localPlayer, WindChangerObj);
        WindChangerObj.transform.position = RespawnPoint.position;
        WindChangerObj.transform.rotation = RespawnPoint.rotation;
    }
}
