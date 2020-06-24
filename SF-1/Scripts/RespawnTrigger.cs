
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RespawnTrigger : UdonSharpBehaviour
{
    public GameObject[] ObjectsSetOff;
    public GameObject[] ObjectsSetOn;
    //public GameObject[] ObjectsToggle;
    public EngineController[] EngineControllerPilotingDisable;
    public GunShipGunController[] GunshipGunControllerManningDisable;
    private void OnTriggerStay(Collider col)
    {
        if (col == null) return;
        if (col.gameObject.layer == 10)
        {
            foreach (var t in ObjectsSetOn)
            {
                t.gameObject.SetActive(true);
            }
            /*foreach (var t in ObjectsToggle)
            {
                t.gameObject.SetActive(!t.gameObject.activeSelf);
            }*/
            foreach (var t in EngineControllerPilotingDisable)
            {
                t.Piloting = false;
                t.Passenger = false;
            }
            foreach (var t in GunshipGunControllerManningDisable)
            {
                t.Manning = false;
            }
            foreach (var t in ObjectsSetOff)
            {
                t.gameObject.SetActive(false);
            }
        }
    }
}