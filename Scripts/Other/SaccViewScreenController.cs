
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class SaccViewScreenController : UdonSharpBehaviour
{
    [Tooltip("Layer to spherecast to find all triggers on to use as follow targets")]
    public LayerMask AAMTargetsLayer;
    [Tooltip("If enabled, disable when player is this many meters away from this gameobject")]
    public float DisableDistance = 15;
    [Tooltip("Camera that follows the planes")]
    public Camera PlaneCamera;
    [Tooltip("Screen that is enabled when turned on")]
    public GameObject ViewScreen;
    public Text ChannelNumberText;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [UdonSynced(UdonSyncMode.None)] public int AAMTarget;
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public bool Disabled = true;
    [System.NonSerializedAttribute] public bool InEditor = true;
    private int currenttarget = -1;
    private SaccEntity TargetEntity;
    private Transform TargetCoM;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null) InEditor = false;
        //get array of AAM Targets
        RaycastHit[] aamtargs = Physics.SphereCastAll(gameObject.transform.position, 1000000, gameObject.transform.forward, 5, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        NumAAMTargets = aamtargs.Length;

        //populate AAMTargets list
        int n = 0;
        foreach (RaycastHit target in aamtargs)
        {
            SaccEntity TargetEntityStart = null;
            GameObject TargObjs = target.collider.gameObject;
            while (TargetEntityStart == null && TargObjs.transform.parent != null)
            {
                TargObjs = TargObjs.transform.parent.gameObject;
                TargetEntityStart = TargObjs.GetComponent<SaccEntity>();
            }

            if (TargetEntityStart != null)
            {
                AAMTargets[n] = TargetEntityStart.gameObject;
                n++;
            }
            else NumAAMTargets -= 1;
        }
        n = 0;
        //create a unique number based on position in the hierarchy in order to sort the AAMTargets array later, to make sure it's the same among clients 
        float[] order = new float[NumAAMTargets];
        for (int i = 0; AAMTargets[n] != null; i++)
        {
            Transform parent = AAMTargets[n].transform;
            for (int x = 0; parent != null; x++)
            {
                order[n] = float.Parse($"{(int)order[n]}{parent.transform.GetSiblingIndex()}");
                parent = parent.transform.parent;
            }
            n++;
        }
        //sort AAMTargets array based on order
        if (NumAAMTargets > 0)
        {
            SortTargets(AAMTargets, order);
        }
    }

    private void Update()
    {
        if (!Disabled)
        {
            //check for change in target
            if (currenttarget != AAMTarget)
            {
                if (AAMTargets[AAMTarget] != null && AAMTargets[AAMTarget].transform.parent != null)
                {
                    TargetEntity = AAMTargets[AAMTarget].GetComponent<SaccEntity>();
                    TargetCoM = TargetEntity.CenterOfMass;
                    PlaneCamera.transform.rotation = TargetEntity.transform.rotation;
                }
            }
            currenttarget = AAMTarget;
            //disable if far away
            if (!InEditor)
            {
                if (Vector3.Distance(localPlayer.GetPosition(), gameObject.transform.position) > DisableDistance)
                {
                    ViewScreen.SetActive(false);
                    PlaneCamera.gameObject.SetActive(false);
                    Disabled = true;
                }
            }
            //        if (TargetEngine.EffectsControl.LargeEffectsOnly)
            //        { TargetEngine.EffectsControl.Effects(); }//this is skipped in effectscontroller as an optimization if plane is distant, but the camera can see it close up, so do it here.

            var VehicleTrans = TargetEntity.transform;
            Quaternion NewRot;
            Vector3 NewPos = VehicleTrans.TransformDirection(new Vector3(0, 14, 0));
            RaycastHit hit;
            if (Physics.Raycast(TargetCoM.position + NewPos, -VehicleTrans.forward, out hit, 50, 1))
            {
                NewPos = hit.point + VehicleTrans.forward * .2f;
                NewRot = VehicleTrans.rotation;
                NewRot = Quaternion.AngleAxis(((-hit.distance + 50) / 50) * 30, VehicleTrans.right) * NewRot;
            }
            else
            {
                NewPos = (TargetCoM.position + NewPos) - (VehicleTrans.forward * 50);
                NewRot = VehicleTrans.rotation;
            }

            PlaneCamera.transform.position = NewPos;
            PlaneCamera.transform.rotation = Quaternion.Slerp(PlaneCamera.transform.rotation, NewRot, 8f * Time.deltaTime);

            ChannelNumberText.text = string.Concat((AAMTarget + 1).ToString(), "\n", TargetEntity.PilotName);
        }
    }

    void SortTargets(GameObject[] Targets, float[] order)
    {
        for (int i = 1; i < order.Length; i++)
        {
            for (int j = 0; j < (order.Length - i); j++)
            {
                if (order[j] > order[j + 1])
                {
                    var h = order[j + 1];
                    order[j + 1] = order[j];
                    order[j] = h;
                    var k = Targets[j + 1];
                    Targets[j + 1] = Targets[j];
                    Targets[j] = k;
                }
            }
        }
    }
}
